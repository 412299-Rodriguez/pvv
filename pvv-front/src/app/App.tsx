import { useEffect } from 'react';

import { WizardPage } from '@/pages/wizard';
import { InvalidPortal } from '@/pages/invalid-portal';
import { MockCheckoutPage } from '@/pages/mock-checkout';
import { PaymentResultPage } from '@/pages/payment-result';
import { resolveCompanyFromUrl, usePvvConfigStore } from '@/entities/company';
import { requestContext } from '@/shared/api';
import { Spinner } from '@/shared/ui';
import styles from './App.module.css';

/**
 * Application root. On mount it bootstraps the tenant: resolves the company from
 * the `?c=` token (or the one persisted across the payment redirect) and loads +
 * applies its theme. It then renders one of three views by URL: the mock checkout
 * (/mock-checkout), the post-payment result (/?tx=...), or the wizard.
 */
export function App() {
  const status = usePvvConfigStore((s) => s.status);
  const loadAndApply = usePvvConfigStore((s) => s.loadAndApply);
  const markInvalid = usePvvConfigStore((s) => s.markInvalid);

  useEffect(() => {
    // ?c= takes precedence; otherwise reuse the token persisted before a redirect.
    const token = resolveCompanyFromUrl() ?? requestContext.companyToken;

    // Dev: Cloudflare's test secret accepts any token; the real Turnstile widget
    // is wired in a later step.
    requestContext.setTurnstileToken(
      import.meta.env.VITE_TURNSTILE_DEV_TOKEN ?? 'dev-turnstile-token',
    );

    if (!token) {
      markInvalid();
      return;
    }
    requestContext.setCompanyToken(token);
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

  const path = window.location.pathname;
  const hasTx = new URLSearchParams(window.location.search).has('tx');

  return (
    <div className={styles.app}>
      {path.startsWith('/mock-checkout') ? (
        <MockCheckoutPage />
      ) : hasTx ? (
        <PaymentResultPage />
      ) : (
        <WizardPage />
      )}
    </div>
  );
}
