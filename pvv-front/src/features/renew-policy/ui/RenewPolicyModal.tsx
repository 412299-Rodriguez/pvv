import { useState } from 'react';

import { useSessionStore } from '@/entities/session';
import { demoExistingPolicy } from '@/entities/policy';
import { Button } from '@/shared/ui';
import styles from './RenewPolicyModal.module.css';

/**
 * Modal shown when the entered plate already has active coverage (demo trigger).
 * Offers to schedule an early renewal starting the day after the current policy
 * expires (pre-selected, editable via a native date picker).
 */
export function RenewPolicyModal() {
  const isOpen = useSessionStore((s) => s.isRenewModalOpen);
  const confirm = useSessionStore((s) => s.confirmRenewal);
  const cancel = useSessionStore((s) => s.cancelRenewal);

  // Local state: the chosen renewal start date (ISO yyyy-mm-dd).
  const [startDate, setStartDate] = useState(demoExistingPolicy.earliestRenewalIso);

  if (!isOpen) return null;

  return (
    <div
      className={styles.overlay}
      role="dialog"
      aria-modal="true"
      aria-labelledby="renew-title"
      onClick={(e) => {
        if (e.target === e.currentTarget) cancel();
      }}
    >
      <div className={styles.modal}>
        <div className={styles.head}>
          <div className={styles.amber} aria-hidden>
            ⚠️
          </div>
          <h3 id="renew-title" className={styles.title}>
            Tu vehículo ya tiene cobertura activa
          </h3>
        </div>

        <div className={styles.policyBox}>
          <div className={styles.policyNumber}>Póliza {demoExistingPolicy.number}</div>
          <div className={styles.policyUntil}>
            Vigente hasta el {demoExistingPolicy.validUntil}
          </div>
        </div>

        <div className={styles.divider} />

        <p className={styles.copy}>
          Podés programar tu nueva póliza para iniciar automáticamente cuando venza la actual.
        </p>

        <div className={styles.dateField}>
          <label htmlFor="renew-date">Fecha de inicio de la nueva póliza</label>
          <input
            id="renew-date"
            type="date"
            className={styles.dateInput}
            value={startDate}
            min={demoExistingPolicy.earliestRenewalIso}
            onChange={(e) => setStartDate(e.target.value)}
          />
        </div>

        <div className={styles.buttons}>
          <Button variant="ghostBorder" onClick={cancel}>
            Cancelar
          </Button>
          <Button variant="primary" onClick={confirm}>
            Continuar con esta fecha →
          </Button>
        </div>
      </div>
    </div>
  );
}
