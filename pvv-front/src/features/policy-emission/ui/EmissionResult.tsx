import { useEffect, useMemo } from 'react';

import { useSessionStore } from '@/entities/session';
import { EMISSION_DURATION_MS } from '@/shared/config';
import { Button, Spinner, DownloadIcon } from '@/shared/ui';
import { formatCurrency } from '@/shared/lib';
import styles from './EmissionResult.module.css';

const CONFETTI_COLORS = ['#6c3ce1', '#00e5ff', '#00c853', '#9c6fff', '#f59e0b'];

/**
 * Step 5 — emission outcome.
 * While `emitting`, runs a timer that resolves the emission. On `success` it
 * shows the issued policy ticket with confetti; on `error`, a retry screen.
 */
export function EmissionResult() {
  const emission = useSessionStore((s) => s.emission);
  const issuedPolicy = useSessionStore((s) => s.issuedPolicy);
  const finishEmission = useSessionStore((s) => s.finishEmission);
  const startEmission = useSessionStore((s) => s.startEmission);
  const reset = useSessionStore((s) => s.reset);

  // Drive the emission timer while in the "emitting" state.
  useEffect(() => {
    if (emission !== 'emitting') return;
    const id = window.setTimeout(() => finishEmission('success'), EMISSION_DURATION_MS);
    return () => window.clearTimeout(id);
  }, [emission, finishEmission]);

  // Stable confetti layout per success render.
  const confetti = useMemo(
    () =>
      Array.from({ length: 20 }, (_, i) => ({
        left: `${Math.random() * 100}%`,
        backgroundColor: CONFETTI_COLORS[i % CONFETTI_COLORS.length],
        borderRadius: i % 2 ? '50%' : '2px',
        animationDelay: `${Math.random() * 0.4}s`,
        animationDuration: `${1 + Math.random() * 1.1}s`,
      })),
    [],
  );

  return (
    <div className={styles.result}>
      {emission === 'emitting' && (
        <div className={styles.state}>
          <Spinner />
          <h2 className={styles.title}>Emitiendo tu póliza...</h2>
          <p className={styles.subtitle}>Esto puede tardar unos segundos</p>
          <div className={styles.bar}>
            <i className={styles.barFill} />
          </div>
        </div>
      )}

      {emission === 'success' && issuedPolicy && (
        <div className={styles.state}>
          <div className={styles.confetti} aria-hidden>
            {confetti.map((style, i) => (
              <i key={i} style={style} />
            ))}
          </div>

          <div className={styles.ring}>
            <svg viewBox="0 0 72 72">
              <circle className={styles.ringCircle} cx="36" cy="36" r="32" />
              <polyline className={styles.ringTick} points="22 37 32 47 51 26" />
            </svg>
          </div>

          <h2 className={styles.titleLg}>¡Tu seguro está listo! 🎉</h2>
          <p className={styles.subtitle}>Te enviamos una copia a tu email</p>

          {/* Policy ticket */}
          <div className={styles.ticket}>
            <div className={styles.ticketHead}>
              <div className={styles.ticketLabel}>Número de póliza</div>
              <div className={styles.ticketNumber}>{issuedPolicy.number}</div>
            </div>
            <div className={styles.ticketBody}>
              <div className={styles.perforation} />
              <div className={styles.ticketGrid}>
                <TicketCell label="Vehículo" value={issuedPolicy.vehicleTitle} />
                <TicketCell label="Titular" value={issuedPolicy.holderName} />
                <TicketCell label="Vigencia desde" value={issuedPolicy.validFrom} />
                <TicketCell label="Vigencia hasta" value={issuedPolicy.validUntil} />
              </div>
              <div className={styles.ticketFoot}>
                <span className={styles.footLabel}>Precio pagado</span>
                <span className={styles.footValue}>{formatCurrency(issuedPolicy.pricePaid)}</span>
              </div>
            </div>
          </div>

          <div className={styles.buttons}>
            <Button
              variant="outline"
              onClick={() => {
                // Placeholder: a real build would generate / download the PDF.
              }}
            >
              <DownloadIcon />
              Descargar póliza PDF
            </Button>
            <Button variant="primary" onClick={reset}>
              Nueva consulta
            </Button>
          </div>
        </div>
      )}

      {emission === 'error' && (
        <div className={styles.state}>
          <div className={styles.errorIcon}>
            <svg viewBox="0 0 24 24" fill="none">
              <line x1="7" y1="7" x2="17" y2="17" stroke="currentColor" strokeWidth="5" strokeLinecap="round" />
              <line x1="17" y1="7" x2="7" y2="17" stroke="currentColor" strokeWidth="5" strokeLinecap="round" />
            </svg>
          </div>
          <h2 className={styles.title}>Hubo un problema con la emisión</h2>
          <p className={styles.subtitle}>
            No pudimos procesar el pago. No se realizó ningún cargo a tu cuenta.
          </p>
          <div className={styles.buttons}>
            <Button variant="outline" onClick={startEmission}>
              Reintentar
            </Button>
            <Button variant="primary" onClick={reset}>
              Nueva consulta
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}

function TicketCell({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <div className={styles.cellLabel}>{label}</div>
      <div className={styles.cellValue}>{value}</div>
    </div>
  );
}
