import { useState } from 'react';

import { useSessionStore, selectSelectedCoverage } from '@/entities/session';
import { vehicleTitle } from '@/entities/vehicle';
import { policyholderFullName } from '@/entities/policyholder';
import { createBudget, startPayment } from '@/shared/api/ingress';
import { useAlertModalStore } from '@/shared/lib';
import { Button, MercadoPagoMark } from '@/shared/ui';
import styles from './PaymentSection.module.css';

/**
 * Step 4 (part 2) — payment method + final CTA.
 * On "Pagar ahora" it creates the soat budget (BUDGET_CALC), opens the checkout
 * preference (PAYMENT_INIT) and redirects to our mock Mercado Pago checkout.
 */
export function PaymentSection() {
  const plate = useSessionStore((s) => s.plate);
  const documentNumber = useSessionStore((s) => s.documentNumber);
  const policyholder = useSessionStore((s) => s.policyholder);
  const vehicle = useSessionStore((s) => s.vehicle);
  const coverage = useSessionStore(selectSelectedCoverage);

  const [processing, setProcessing] = useState(false);
  const showAlert = useAlertModalStore((s) => s.showAlert);

  const handlePay = async () => {
    if (!coverage || !vehicle) return;
    setProcessing(true);
    try {
      const { budgetId, amount } = await createBudget({
        plate,
        dni: documentNumber,
        firstName: policyholder.firstName,
        lastName: policyholder.lastName,
        email: policyholder.email,
        phone: policyholder.phone,
        productId: coverage.id,
        price: coverage.pricePerYear,
      });
      const { initPoint } = await startPayment({
        budgetId,
        amount,
        vehicleTitle: vehicleTitle(vehicle),
        holderName: policyholderFullName(policyholder),
      });
      // Hand off to the (mock) Mercado Pago checkout; it returns to /?tx=...
      window.location.href = initPoint;
    } catch {
      setProcessing(false);
      showAlert('No pudimos iniciar el pago', 'Ocurrió un problema. Intentá de nuevo en unos segundos.');
    }
  };

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

      <div className={styles.payBar}>
        <Button variant="mercadoPago" fullWidth disabled={processing} onClick={handlePay}>
          {processing ? (
            <>
              <span className={styles.btnSpinner} />
              Redirigiendo...
            </>
          ) : (
            <>
              <MercadoPagoMark swooshColor="var(--color-primary)" />
              Pagar ahora
            </>
          )}
        </Button>
      </div>
    </section>
  );
}
