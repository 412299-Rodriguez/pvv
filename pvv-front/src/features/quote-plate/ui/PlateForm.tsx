import type { ReactNode } from 'react';
import { useSessionStore, selectCanSubmitPlate } from '@/entities/session';
import { Card, Button, Checkbox, CheckIcon, LockIcon } from '@/shared/ui';
import { normalizePlate, isPlateValid } from '@/shared/lib';
import styles from './PlateForm.module.css';

/**
 * Step 1 — plate entry.
 * Validates the plate live, requires T&C acceptance, and submits to advance to
 * the document step (or open the renew modal for the demo trigger plate).
 */
export function PlateForm() {
  const plate = useSessionStore((s) => s.plate);
  const termsAccepted = useSessionStore((s) => s.termsAccepted);
  const setPlate = useSessionStore((s) => s.setPlate);
  const toggleTerms = useSessionStore((s) => s.toggleTerms);
  const submitPlate = useSessionStore((s) => s.submitPlate);
  const canSubmit = useSessionStore(selectCanSubmitPlate);

  const plateOk = isPlateValid(plate);

  return (
    <Card>
      <div className={styles.title}>Ingresá la patente</div>
      <div className={styles.subtitle}>Validamos los datos al instante</div>

      {/* License-plate styled input */}
      <div className={styles.plate}>
        <input
          className={styles.plateInput}
          value={plate}
          maxLength={9}
          placeholder="ABC 123"
          autoComplete="off"
          spellCheck={false}
          onChange={(e) => setPlate(normalizePlate(e.target.value))}
        />
      </div>

      <div className={styles.terms}>
        <Checkbox checked={termsAccepted} onChange={toggleTerms}>
          Acepto los{' '}
          <a href="#" onClick={(e) => e.preventDefault()}>
            Términos y Condiciones
          </a>{' '}
          y la{' '}
          <a href="#" onClick={(e) => e.preventDefault()}>
            Política de Privacidad
          </a>
        </Checkbox>

        <div className={styles.checklist}>
          <ChecklistItem ok={plateOk}>Patente válida (6-7 caracteres)</ChecklistItem>
          <ChecklistItem ok={termsAccepted}>Términos aceptados</ChecklistItem>
        </div>
      </div>

      <Button
        variant="primary"
        fullWidth
        className={styles.cta}
        disabled={!canSubmit}
        onClick={submitPlate}
      >
        Cotizar mi seguro →
      </Button>

      <div className={styles.secureNote}>
        <LockIcon />
        Tus datos están protegidos
      </div>
    </Card>
  );
}

function ChecklistItem({ ok, children }: { ok: boolean; children: ReactNode }) {
  return (
    <div className={[styles.clItem, ok ? styles.clOk : ''].filter(Boolean).join(' ')}>
      <span className={styles.clTick}>
        <CheckIcon />
      </span>
      {children}
    </div>
  );
}
