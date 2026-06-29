import { useEffect, useState } from 'react';

import { getEmissionStatus, confirmMockPayment } from '@/shared/api/ingress';
import { requestContext } from '@/shared/api';
import { formatCurrency } from '@/shared/lib';
import { Button, MercadoPagoMark, Spinner } from '@/shared/ui';
import styles from './MockCheckoutPage.module.css';

/** Keep the "connecting" loader up at least this long so the redirect isn't abrupt. */
const MIN_LOADER_MS = 1300;

/**
 * Our own mock Mercado Pago checkout — the page PAYMENT_INIT redirects to. Stands
 * in for the real provider: "Aprobar/Rechazar" calls our webhook and returns to
 * the result page (/?tx=...), where emission is polled.
 */
export function MockCheckoutPage() {
  const tx = new URLSearchParams(window.location.search).get('tx') ?? '';
  const [amount, setAmount] = useState<number | null>(null);
  const [vehicleTitle, setVehicleTitle] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    let timer = 0;
    const startedAt = Date.now();
    // Hide the loader only after the min time, so the screen change is smooth.
    const settle = () => {
      const wait = Math.max(0, MIN_LOADER_MS - (Date.now() - startedAt));
      timer = window.setTimeout(() => {
        if (active) setLoading(false);
      }, wait);
    };

    if (!tx) {
      settle();
      return () => {
        active = false;
        window.clearTimeout(timer);
      };
    }

    getEmissionStatus(tx)
      .then((status) => {
        if (!active) return;
        setAmount(status.amount);
        setVehicleTitle(status.vehicleTitle);
      })
      .catch(() => {
        /* show the buttons anyway */
      })
      .finally(settle);

    return () => {
      active = false;
      window.clearTimeout(timer);
    };
  }, [tx]);

  const finish = async (approved: boolean) => {
    setBusy(true);
    try {
      await confirmMockPayment(tx, approved);
    } catch {
      /* still return to the result page, which will reflect the state */
    }
    const token = requestContext.companyToken;
    const company = token ? `&c=${encodeURIComponent(token)}` : '';
    window.location.href = `/?tx=${tx}${company}`;
  };

  if (loading) {
    return (
      <div className={styles.page}>
        <div className={styles.loader}>
          <div className={styles.brand}>
            <MercadoPagoMark swooshColor="#009ee3" />
            <span>Checkout</span>
          </div>
          <Spinner />
          <p className={styles.loaderText}>Conectando con el medio de pago…</p>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.page}>
      <div className={styles.card}>
        <div className={styles.brand}>
          <MercadoPagoMark swooshColor="#009ee3" />
          <span>Checkout</span>
        </div>
        <div className={styles.badge}>Entorno de prueba</div>

        <h1 className={styles.title}>Confirmá tu pago</h1>
        {vehicleTitle && <p className={styles.sub}>Seguro para {vehicleTitle}</p>}
        {amount !== null && <div className={styles.amount}>{formatCurrency(amount)}</div>}

        <div className={styles.buttons}>
          <Button variant="primary" fullWidth disabled={busy || !tx} onClick={() => finish(true)}>
            Aprobar pago
          </Button>
          <Button variant="outline" fullWidth disabled={busy || !tx} onClick={() => finish(false)}>
            Rechazar
          </Button>
        </div>

        <p className={styles.note}>
          Simulación de Mercado Pago — no se realiza ningún cargo real.
        </p>
      </div>
    </div>
  );
}
