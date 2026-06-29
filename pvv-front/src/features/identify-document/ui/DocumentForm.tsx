import { useState } from 'react';
import { useSessionStore } from '@/entities/session';
import { DOCUMENT_TYPES } from '@/entities/policyholder';
import { lookupHolder } from '@/shared/api/ingress';
import { Card, SegmentedControl, TextField, Button, UserIcon } from '@/shared/ui';
import { digitsOnly } from '@/shared/lib';
import styles from './DocumentForm.module.css';

/**
 * Step 2 — identify the policyholder by document.
 * "Continuar" simulates a contact lookup (pre-fills demo data); the secondary
 * link continues without a lookup so the user fills everything manually.
 */
export function DocumentForm() {
  const documentType = useSessionStore((s) => s.documentType);
  const documentNumber = useSessionStore((s) => s.documentNumber);
  const setDocumentType = useSessionStore((s) => s.setDocumentType);
  const setDocumentNumber = useSessionStore((s) => s.setDocumentNumber);
  const applyHolderLookup = useSessionStore((s) => s.applyHolderLookup);
  const skipHolderLookup = useSessionStore((s) => s.skipHolderLookup);

  const [searching, setSearching] = useState(false);

  // "Continuar": run the (mock) holder lookup and pre-fill personal data.
  const handleSearch = async () => {
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
      <h2 className={styles.title}>¿Quién es el titular?</h2>
      <p className={styles.subtitle}>Ingresá tu documento para personalizar la cotización</p>

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

      <Button variant="primary" fullWidth disabled={searching} onClick={handleSearch}>
        {searching ? 'Buscando…' : 'Continuar →'}
      </Button>

      <div className={styles.linkRow}>
        <a
          href="#"
          onClick={(e) => {
            e.preventDefault();
            if (!searching) skipHolderLookup();
          }}
        >
          No tengo documento a mano → Continuar sin buscar
        </a>
      </div>
    </Card>
  );
}
