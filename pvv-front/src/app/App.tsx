import { useEffect } from 'react';

import { WizardPage } from '@/pages/wizard';
import { InvalidPortal } from '@/pages/invalid-portal';
import { resolveCompanyFromUrl, usePvvConfigStore } from '@/entities/company';
import { requestContext } from '@/shared/api';
import { Spinner } from '@/shared/ui';
import styles from './App.module.css';

/**
 * Application root. On mount it bootstraps the tenant: resolves the company from
 * the `?c=` URL token and loads + applies its theme. Without a valid company it
 * shows a friendly "not a valid portal" screen instead of the wizard.
 */
export function App() {
  const status = usePvvConfigStore((s) => s.status);
  const loadAndApply = usePvvConfigStore((s) => s.loadAndApply);
  const markInvalid = usePvvConfigStore((s) => s.markInvalid);

  useEffect(() => {
    const token = resolveCompanyFromUrl();

    // Dev: Cloudflare's test secret accepts any token; the real Turnstile widget
    // is wired in a later step.
    requestContext.setTurnstileToken(
      import.meta.env.VITE_TURNSTILE_DEV_TOKEN ?? 'dev-turnstile-token',
    );

    if (!token) {
      markInvalid();
      return;
    }
    void loadAndApply();
  }, [loadAndApply, markInvalid]);

  if (status === 'loading') {
    return (
      <div className={styles.loader}>
        <Spinner />
      </div>
    );
  }

  if (status === 'invalid') {
    return <InvalidPortal />;
  }

  return (
    <div className={styles.app}>
      <WizardPage />
    </div>
  );
}
