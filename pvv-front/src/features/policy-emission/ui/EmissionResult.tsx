import { useMemo } from 'react';

import { Button, Spinner } from '@/shared/ui';
import { formatCurrency } from '@/shared/lib';
import styles from './EmissionResult.module.css';

const CONFETTI_COLORS = ['#6c3ce1', '#00e5ff', '#00c853', '#9c6fff', '#f59e0b'];

export interface EmissionTicket {
  number: string;
  vehicleTitle: string;
  holderName: string;
  validFrom: string;
  validUntil: string;
  pricePaid: number;
}

interface EmissionResultProps {
  state: 'emitting' | 'success' | 'error';
  ticket?: EmissionTicket | null;
  onHome: () => void;
  onRetry?: () => void;
}

/**
 * Presentational emission outcome screen. Driven entirely by props so it can be
 * fed by the result page's EMISSION_STATUS polling (after the payment redirect).
 */
export function EmissionResult({ state, ticket, onHome, onRetry }: EmissionResultProps) {
  // Stable confetti layout per render.
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
      {state === 'emitting' && (
        <div className={styles.state}>
          <Spinner />
          <h2 className={styles.title}>Emitiendo tu póliza...</h2>
          <p className={styles.subtitle}>Esto puede tardar unos segundos</p>
          <div className={styles.bar}>
            <i className={styles.barFill} />
          </div>
        </div>
      )}

      {state === 'success' && ticket && (
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

          {/* Policy ticket */}
          <div className={styles.ticket}>
            <div className={styles.ticketHead}>
              <div className={styles.ticketLabel}>Número de póliza</div>
              <div className={styles.ticketNumber}>{ticket.number}</div>
            </div>
            <div className={styles.ticketBody}>
              <div className={styles.perforation} />
              <div className={styles.ticketGrid}>
                <TicketCell label="Vehículo" value={ticket.vehicleTitle} />
                <TicketCell label="Titular" value={ticket.holderName} />
                <TicketCell label="Vigencia desde" value={ticket.validFrom} />
                <TicketCell label="Vigencia hasta" value={ticket.validUntil} />
              </div>
              <div className={styles.ticketFoot}>
                <span className={styles.footLabel}>Precio pagado</span>
                <span className={styles.footValue}>{formatCurrency(ticket.pricePaid)}</span>
              </div>
            </div>
          </div>

          <div className={styles.buttons}>
            <Button variant="primary" onClick={onHome}>
              Volver al inicio
            </Button>
          </div>
        </div>
      )}

      {state === 'error' && (
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
            {onRetry && (
              <Button variant="outline" onClick={onRetry}>
                Reintentar
              </Button>
            )}
            <Button variant="primary" onClick={onHome}>
              Volver al inicio
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
