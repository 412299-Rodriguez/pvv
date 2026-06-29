import { useEffect } from 'react';

import { WizardPage } from '@/pages/wizard';
import { resolveCompanyFromUrl, usePvvConfigStore } from '@/entities/company';
import { requestContext } from '@/shared/api';
import styles from './App.module.css';

/**
 * Application root. On mount it bootstraps the tenant: resolves the company from
 * the `?c=` URL token, primes the Turnstile token, and loads + applies the
 * company's theme (colors/texts) before showing the wizard.
 */
export function App() {
  const loadAndApply = usePvvConfigStore((s) => s.loadAndApply);

  useEffect(() => {
    resolveCompanyFromUrl();

    // Dev: Cloudflare's test secret accepts any token; the real Turnstile widget
    // is wired in a later step.
    requestContext.setTurnstileToken(
      import.meta.env.VITE_TURNSTILE_DEV_TOKEN ?? 'dev-turnstile-token',
    );

    // Best-effort: keep the default (design-token) theme if config can't load.
    loadAndApply().catch(() => undefined);
  }, [loadAndApply]);

  return (
    <div className={styles.app}>
      <WizardPage />
    </div>
  );
}
