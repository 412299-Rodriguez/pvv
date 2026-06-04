#!/usr/bin/env sh
# PVV — RabbitMQ setup script
# Declares the pvv vhost and the emission queues via the management HTTP API.
# The vhost and user are normally created by the RABBITMQ_DEFAULT_* env vars in
# docker-compose; this script is idempotent and also (re)declares them so it can
# be run manually against a fresh broker.
#
# Usage (manual):
#   RABBITMQ_HOST=localhost ./rabbitmq-setup.sh

set -eu

RABBITMQ_HOST="${RABBITMQ_HOST:-localhost}"
RABBITMQ_MGMT_PORT="${RABBITMQ_MGMT_PORT:-15672}"
RABBITMQ_USER="${RABBITMQ_USER:-pvv_user}"
RABBITMQ_PASS="${RABBITMQ_PASS:-pvv_pass}"
RABBITMQ_VHOST="${RABBITMQ_VHOST:-pvv}"

API="http://${RABBITMQ_HOST}:${RABBITMQ_MGMT_PORT}/api"
AUTH="-u ${RABBITMQ_USER}:${RABBITMQ_PASS}"
HDR="content-type:application/json"

# URL-encode the vhost ("/" would become %2F, but "pvv" needs no encoding).
VHOST_ENC="${RABBITMQ_VHOST}"

echo "[rabbitmq-setup] Waiting for management API at ${API} ..."
until curl -sf ${AUTH} "${API}/overview" >/dev/null 2>&1; do
  echo "[rabbitmq-setup] still waiting..."
  sleep 3
done
echo "[rabbitmq-setup] Management API is up."

# 1. Ensure the vhost exists (no-op if created by RABBITMQ_DEFAULT_VHOST).
echo "[rabbitmq-setup] Ensuring vhost '${RABBITMQ_VHOST}'..."
curl -sf ${AUTH} -H "${HDR}" -X PUT "${API}/vhosts/${VHOST_ENC}" >/dev/null

# 2. Grant the application user full permissions on the vhost.
echo "[rabbitmq-setup] Granting permissions to '${RABBITMQ_USER}' on '${RABBITMQ_VHOST}'..."
curl -sf ${AUTH} -H "${HDR}" -X PUT \
  "${API}/permissions/${VHOST_ENC}/${RABBITMQ_USER}" \
  -d '{"configure":".*","write":".*","read":".*"}' >/dev/null

# 3. Declare the dead-letter queue first (it is the dead-letter target).
echo "[rabbitmq-setup] Declaring queue 'pvv_emission_dlq'..."
curl -sf ${AUTH} -H "${HDR}" -X PUT \
  "${API}/queues/${VHOST_ENC}/pvv_emission_dlq" \
  -d '{"durable":true,"auto_delete":false,"arguments":{}}' >/dev/null

# 4. Declare the main emission queue, dead-lettering to pvv_emission_dlq via the
#    default exchange (routing key = queue name).
echo "[rabbitmq-setup] Declaring queue 'pvv_emission_queue'..."
curl -sf ${AUTH} -H "${HDR}" -X PUT \
  "${API}/queues/${VHOST_ENC}/pvv_emission_queue" \
  -d '{"durable":true,"auto_delete":false,"arguments":{"x-dead-letter-exchange":"","x-dead-letter-routing-key":"pvv_emission_dlq"}}' >/dev/null

echo "[rabbitmq-setup] Done. Queues 'pvv_emission_queue' and 'pvv_emission_dlq' are ready on vhost '${RABBITMQ_VHOST}'."
