import { useEffect, useState } from 'react';

import { useSessionStore } from '@/entities/session';
import { PAYMENT_PROCESSING_MS } from '@/shared/config';
import { Button, MercadoPagoMark } from '@/shared/ui';
import styles from './PaymentSection.module.css';

/**
 * Step 4 (part 2) — payment method + final CTA.
 * Renders once a coverage is selected. The pay button shows a short processing
 * spinner (simulating the Mercado Pago redirect) and then kicks off emission.
 */
export function PaymentSection() {
  const startEmission = useSessionStore((s) => s.startEmission);
  const [processing, setProcessing] = useState(false);

  // Simulate the payment gateway round-trip, then start policy emission.
  useEffect(() => {
    if (!processing) return;
    const id = window.setTimeout(startEmission, PAYMENT_PROCESSING_MS);
    return () => window.clearTimeout(id);
  }, [processing, startEmission]);

  return (
    <section className={styles.section}>
      <div className={styles.heading}>¿Cómo querés pagar?</div>

      {/* Single, pre-selected payment method (Mercado Pago). */}
      <div className={`${styles.mpCard} ${styles.selected}`}>
        <div className={styles.mpCircle}>
          <MercadoPagoMark swooshColor="#009ee3" />
        </div>
        <div className={styles.mpText}>
          <b>Mercado Pago</b>
          <span>Pagá con tarjeta de crédito, débito o dinero en cuenta</span>
          <div className={styles.networks}>
            <span className={styles.net} style={{ backgroundColor: 'var(--color-visa)' }}>
              VISA
            </span>
            <span className={styles.net} style={{ backgroundColor: 'var(--color-mastercard)' }}>
              MC
            </span>
            <span className={styles.net} style={{ backgroundColor: 'var(--color-amex)' }}>
              AMEX
            </span>
          </div>
        </div>
        <div className={styles.mpCheck} aria-hidden>
          <svg viewBox="0 0 24 24" fill="none">
            <polyline
              points="4 12 9 17 20 6"
              stroke="#fff"
              strokeWidth="3.5"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
        </div>
      </div>

      <Button
        variant="mercadoPago"
        fullWidth
        disabled={processing}
        onClick={() => setProcessing(true)}
      >
        {processing ? (
          <>
            <span className={styles.btnSpinner} />
            Procesando...
          </>
        ) : (
          <>
            <MercadoPagoMark swooshColor="var(--color-primary)" />
            Pagar con Mercado Pago →
          </>
        )}
      </Button>
    </section>
  );
}
