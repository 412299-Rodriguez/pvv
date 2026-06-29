import { useState } from 'react';
import { useSessionStore } from '@/entities/session';
import { DOCUMENT_TYPES } from '@/entities/policyholder';
import { lookupHolder } from '@/shared/api/ingress';
import { Card, SegmentedControl, TextField, Button, UserIcon } from '@/shared/ui';
import { digitsOnly } from '@/shared/lib';
import { useText } from '@/entities/company';
import styles from './DocumentForm.module.css';

/**
 * Step 2 — identify the policyholder by document.
 * A document number is required; "Continuar" runs the (mock) contact lookup and
 * pre-fills the personal-data form.
 */
export function DocumentForm() {
  const documentType = useSessionStore((s) => s.documentType);
  const documentNumber = useSessionStore((s) => s.documentNumber);
  const setDocumentType = useSessionStore((s) => s.setDocumentType);
  const setDocumentNumber = useSessionStore((s) => s.setDocumentNumber);
  const applyHolderLookup = useSessionStore((s) => s.applyHolderLookup);
  const goBack = useSessionStore((s) => s.goBack);

  const [searching, setSearching] = useState(false);

  // A document is mandatory to continue (min length covers a short DNI).
  const canContinue = documentNumber.trim().length >= 7;

  // "Continuar": run the (mock) holder lookup and pre-fill personal data.
  const handleSearch = async () => {
    if (!canContinue) return;
    setSearching(true);
    try {
      const result = await lookupHolder({ documentType, documentNumber });
      applyHolderLookup(result);
    } finally {
      setSearching(false);
    }
  };

  return (
    <Card>
      <div className={styles.personaIcon}>
        <UserIcon />
      </div>
      <h2 className={styles.title}>{useText('holderTitle', '¿Quién es el titular?')}</h2>
      <p className={styles.subtitle}>
        {useText('holderSubtitle', 'Ingresá tu documento para personalizar la cotización')}
      </p>

      <div className={styles.field}>
        <label className={styles.fieldLabel}>Tipo de documento</label>
        <SegmentedControl
          options={DOCUMENT_TYPES}
          value={documentType}
          onChange={setDocumentType}
          ariaLabel="Tipo de documento"
        />
      </div>

      <TextField
        id="document-number"
        label="Número de documento"
        inputMode="numeric"
        placeholder="12345678"
        maxLength={11}
        autoComplete="off"
        value={documentNumber}
        onChange={(e) => setDocumentNumber(digitsOnly(e.target.value))}
        className={styles.field}
      />

      <div className={styles.actions}>
        <Button variant="ghostBorder" onClick={goBack}>
          ← Volver
        </Button>
        <Button
          variant="primary"
          className={styles.grow}
          disabled={searching || !canContinue}
          onClick={handleSearch}
        >
          {searching ? 'Buscando…' : 'Continuar →'}
        </Button>
      </div>
    </Card>
  );
}
