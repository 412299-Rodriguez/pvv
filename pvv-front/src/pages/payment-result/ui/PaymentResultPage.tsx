import { useEffect, useState } from 'react';

import { getEmissionStatus, syncPayment } from '@/shared/api/ingress';
import { requestContext } from '@/shared/api';
import { EmissionResult, type EmissionTicket } from '@/features/policy-emission';
import { formatDate } from '@/shared/lib';
import styles from './PaymentResultPage.module.css';

type ResultState = 'emitting' | 'awaiting-payment' | 'not-paid' | 'success' | 'error';

const POLL_MS = 2000;
/** Keep the "Emitiendo…" screen up at least this long so it's actually visible. */
const MIN_EMITTING_MS = 2600;
/**
 * How long a transaction may stay Pending before we call the purchase off.
 *
 * Returning here without having paid is indistinguishable, for the first instants,
 * from having paid a moment ago: the provider indexes its own payment with a small
 * delay, so "no payment found" is only meaningful once we have given it time to
 * appear. Past this window, Pending means the buyer did not pay.
 */
const UNPAID_GRACE_MS = 12000;

/**
 * Post-payment result page (/?tx=...). Polls EMISSION_STATUS until the policy is
 * emitted (or fails) and renders the ticket — no prior session state needed, all
 * the ticket data comes back denormalized from the transaction.
 */
export function PaymentResultPage() {
  const tx = new URLSearchParams(window.location.search).get('tx') ?? '';
  const [state, setState] = useState<ResultState>('emitting');
  const [ticket, setTicket] = useState<EmissionTicket | null>(null);
  const [paymentDeadline, setPaymentDeadline] = useState<string | null>(null);

  useEffect(() => {
    if (!tx) {
      setState('error');
      return;
    }

    let active = true;
    let timer = 0;
    const startedAt = Date.now();

    // Settle to a terminal state, but not before MIN_EMITTING_MS so the loader shows.
    const settle = (apply: () => void) => {
      const wait = Math.max(0, MIN_EMITTING_MS - (Date.now() - startedAt));
      timer = window.setTimeout(() => {
        if (active) apply();
      }, wait);
    };

    const poll = async () => {
      try {
        const status = await getEmissionStatus(tx);
        if (!active) return;

        if (status.paymentStatus === 'Failed' || status.paymentStatus === 'Abandoned') {
          settle(() => setState('error'));
          return;
        }

        // A payment exists at the provider but has not been completed: a cash coupon
        // or a transfer, which can take days. Terminal for this page — polling for it
        // would spin forever, and the buyer has somewhere to be (a payment counter).
        if (status.paymentStatus === 'Pending' && status.paymentPendingUntil) {
          const until = status.paymentPendingUntil;
          settle(() => {
            setPaymentDeadline(formatDate(new Date(until)));
            setState('awaiting-payment');
          });
          return;
        }

        if (status.emissionStatus === 'success') {
          // Prefer soat's real coverage window (future-dated on renewals);
          // fall back to a computed year if it isn't available yet.
          const from = new Date(status.validFrom ?? status.emissionUpdatedAt ?? new Date().toISOString());
          const until = status.validUntil
            ? new Date(status.validUntil)
            : (() => {
                const u = new Date(from);
                u.setFullYear(u.getFullYear() + 1);
                return u;
              })();
          settle(() => {
            setTicket({
              number: status.policyNumber ?? '—',
              vehicleTitle: status.vehicleTitle ?? '—',
              holderName: status.holderName ?? '—',
              validFrom: formatDate(from),
              validUntil: formatDate(until),
              pricePaid: status.amount,
            });
            setState('success');
          });
          return;
        }

        if (status.emissionStatus === 'failed' || status.emissionStatus === 'retry-exhausted') {
          settle(() => setState('error'));
          return;
        }

        // Nothing has settled yet, and there are two very different reasons to be
        // here. If the payment is Confirmed the policy is genuinely on its way, and
        // that may legitimately take minutes (the worker retries) — it ends on its
        // own, so it needs no deadline. A Pending payment with nothing at the
        // provider is the one that never ends by itself: it is what a buyer who
        // pressed "volver al sitio" without paying leaves behind.
        if (status.paymentStatus === 'Pending') {
          if (Date.now() - startedAt > UNPAID_GRACE_MS) {
            settle(() => setState('not-paid'));
            return;
          }
          // Ask the provider again instead of only re-reading our own database:
          // while the payment is unconfirmed, nothing here can change on its own
          // unless their notification happens to land.
          await syncPayment(tx).catch(() => undefined);
          if (!active) return;
        }

        timer = window.setTimeout(poll, POLL_MS);
      } catch {
        if (active) timer = window.setTimeout(poll, POLL_MS + 500);
      }
    };

    // Reconcile first, then poll. The payment may already be approved on the
    // provider's side and still unknown here — their notification cannot reach a
    // developer's machine, and in production it can simply arrive after the buyer
    // does. Polling before asking would just watch a Pending transaction.
    void syncPayment(tx).then(() => {
      if (active) void poll();
    });

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
      <EmissionResult
        state={state}
        ticket={ticket}
        paymentDeadline={paymentDeadline}
        onHome={goHome}
      />
    </main>
  );
}
