import { useEffect, useState } from 'react';

import { getEmissionStatus } from '@/shared/api/ingress';
import { requestContext } from '@/shared/api';
import { EmissionResult, type EmissionTicket } from '@/features/policy-emission';
import { formatDate } from '@/shared/lib';
import styles from './PaymentResultPage.module.css';

type ResultState = 'emitting' | 'success' | 'error';

const POLL_MS = 2000;

/**
 * Post-payment result page (/?tx=...). Polls EMISSION_STATUS until the policy is
 * emitted (or fails) and renders the ticket — no prior session state needed, all
 * the ticket data comes back denormalized from the transaction.
 */
export function PaymentResultPage() {
  const tx = new URLSearchParams(window.location.search).get('tx') ?? '';
  const [state, setState] = useState<ResultState>('emitting');
  const [ticket, setTicket] = useState<EmissionTicket | null>(null);

  useEffect(() => {
    if (!tx) {
      setState('error');
      return;
    }

    let active = true;
    let timer = 0;

    const poll = async () => {
      try {
        const status = await getEmissionStatus(tx);
        if (!active) return;

        if (status.paymentStatus === 'Failed' || status.paymentStatus === 'Abandoned') {
          setState('error');
          return;
        }

        if (status.emissionStatus === 'success') {
          const from = new Date(status.emissionUpdatedAt ?? new Date().toISOString());
          const until = new Date(from);
          until.setFullYear(until.getFullYear() + 1);
          setTicket({
            number: status.policyNumber ?? '—',
            vehicleTitle: status.vehicleTitle ?? '—',
            holderName: status.holderName ?? '—',
            validFrom: formatDate(from),
            validUntil: formatDate(until),
            pricePaid: status.amount,
          });
          setState('success');
          return;
        }

        if (status.emissionStatus === 'failed' || status.emissionStatus === 'retry-exhausted') {
          setState('error');
          return;
        }

        // pending / emitting → keep polling
        timer = window.setTimeout(poll, POLL_MS);
      } catch {
        if (active) timer = window.setTimeout(poll, POLL_MS + 500);
      }
    };

    void poll();
    return () => {
      active = false;
      window.clearTimeout(timer);
    };
  }, [tx]);

  const goHome = () => {
    const token = requestContext.companyToken;
    window.location.href = token ? `/?c=${encodeURIComponent(token)}` : '/';
  };

  return (
    <main className={styles.stage}>
      <EmissionResult state={state} ticket={ticket} onHome={goHome} />
    </main>
  );
}
