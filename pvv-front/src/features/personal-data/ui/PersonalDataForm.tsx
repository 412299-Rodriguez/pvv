import { useState } from 'react';

import { useSessionStore } from '@/entities/session';
import type { Policyholder } from '@/entities/policyholder';
import { Card, TextField, Button, MailIcon } from '@/shared/ui';
import { isNonEmptyName, isValidEmail, isValidPhone, digitsOnly } from '@/shared/lib';
import { useBIStore } from '@/shared/analytics';
import styles from './PersonalDataForm.module.css';

/** Per-field validation rules and their error copy. */
const RULES: Record<keyof Policyholder, { validate: (v: string) => boolean; error: string }> = {
  firstName: { validate: isNonEmptyName, error: 'Ingresá tu nombre' },
  lastName: { validate: isNonEmptyName, error: 'Ingresá tu apellido' },
  email: { validate: isValidEmail, error: 'Ingresá un email válido' },
  phone: { validate: isValidPhone, error: 'Ingresá un teléfono válido' },
};

type FieldFlags = Partial<Record<keyof Policyholder, boolean>>;

/**
 * Step 3 — personal data with inline validation.
 * When a contact was found, fields render pre-filled (highlighted) until edited.
 */
export function PersonalDataForm() {
  const contactFound = useSessionStore((s) => s.contactFound);
  const policyholder = useSessionStore((s) => s.policyholder);
  const documentNumber = useSessionStore((s) => s.documentNumber);
  const track = useBIStore((s) => s.track);
  const setField = useSessionStore((s) => s.setPolicyholderField);
  const submit = useSessionStore((s) => s.submitPersonalData);
  const goBack = useSessionStore((s) => s.goBack);

  // Local UI concerns: which fields have been touched/edited and shown errors.
  const [showErrors, setShowErrors] = useState<FieldFlags>({});
  const [edited, setEdited] = useState<FieldFlags>({});

  const errorFor = (field: keyof Policyholder): string | undefined => {
    if (!showErrors[field]) return undefined;
    return RULES[field].validate(policyholder[field]) ? undefined : RULES[field].error;
  };

  const handleChange = (field: keyof Policyholder) => (value: string) => {
    setField(field, value);
    setEdited((prev) => ({ ...prev, [field]: true }));
  };

  const handleBlur = (field: keyof Policyholder) => () =>
    setShowErrors((prev) => ({ ...prev, [field]: true }));

  const handleSubmit = () => {
    const allValid = (Object.keys(RULES) as (keyof Policyholder)[]).every((f) =>
      RULES[f].validate(policyholder[f]),
    );
    if (!allValid) {
      // Reveal every error at once.
      setShowErrors({ firstName: true, lastName: true, email: true, phone: true });
      return;
    }
    // Funnel step 2: from here on we know who the buyer is and how to reach them.
    track('holder_completed', {
      firstName: policyholder.firstName,
      lastName: policyholder.lastName,
      dni: documentNumber,
      email: policyholder.email,
      phone: policyholder.phone,
    });
    submit();
  };

  return (
    <Card>
      {contactFound && (
        <div className={styles.foundBanner}>
          <CheckCircle />
          Encontramos tus datos — Verificalos antes de continuar
        </div>
      )}

      <div className={styles.rowHead}>
        <h2 className={styles.title}>Completá tus datos</h2>
        <span className={styles.miniBadge}>Paso 2 de 3</span>
      </div>

      <div className={styles.formGrid}>
        <TextField
          id="firstName"
          label="Nombre *"
          placeholder="Tu nombre"
          autoComplete="off"
          value={policyholder.firstName}
          prefilled={contactFound && !edited.firstName}
          error={errorFor('firstName')}
          onChange={(e) => handleChange('firstName')(e.target.value)}
          onBlur={handleBlur('firstName')}
        />
        <TextField
          id="lastName"
          label="Apellido *"
          placeholder="Tu apellido"
          autoComplete="off"
          value={policyholder.lastName}
          prefilled={contactFound && !edited.lastName}
          error={errorFor('lastName')}
          onChange={(e) => handleChange('lastName')(e.target.value)}
          onBlur={handleBlur('lastName')}
        />
        <TextField
          id="email"
          label="Email *"
          type="email"
          inputMode="email"
          placeholder="tu@email.com"
          autoComplete="off"
          adornment={<MailIcon />}
          value={policyholder.email}
          prefilled={contactFound && !edited.email}
          error={errorFor('email')}
          onChange={(e) => handleChange('email')(e.target.value)}
          onBlur={handleBlur('email')}
        />
        <TextField
          id="phone"
          label="Teléfono *"
          placeholder="911 2345678"
          inputMode="numeric"
          autoComplete="off"
          prefix="+54"
          maxLength={14}
          value={policyholder.phone}
          prefilled={contactFound && !edited.phone}
          error={errorFor('phone')}
          onChange={(e) => handleChange('phone')(digitsOnly(e.target.value).slice(0, 14))}
          onBlur={handleBlur('phone')}
        />
      </div>

      <div className={styles.actions}>
        <Button variant="ghostBorder" onClick={goBack}>
          ← Volver
        </Button>
        <Button variant="primary" className={styles.grow} onClick={handleSubmit}>
          Ver mi cotización →
        </Button>
      </div>
    </Card>
  );
}

/** Small filled check-in-circle used by the "found" banner. */
function CheckCircle() {
  return (
    <svg viewBox="0 0 24 24" fill="none" className={styles.bannerIcon}>
      <circle cx="12" cy="12" r="9.5" stroke="currentColor" strokeWidth="2" />
      <polyline
        points="8 12.5 11 15.5 16.5 9"
        stroke="currentColor"
        strokeWidth="2.2"
        fill="none"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}
