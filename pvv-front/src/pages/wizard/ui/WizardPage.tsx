import { useSessionStore } from '@/entities/session';
import { VehicleCard } from '@/entities/vehicle';

import { Stepper } from '@/widgets/stepper';
import { PurchaseSummary } from '@/widgets/purchase-summary';

import { PlateForm } from '@/features/quote-plate';
import { DocumentForm } from '@/features/identify-document';
import { PersonalDataForm } from '@/features/personal-data';
import { CoverageSelector } from '@/features/select-coverage';
import { PaymentSection } from '@/features/checkout';
import { EmissionResult } from '@/features/policy-emission';
import { RenewPolicyModal } from '@/features/renew-policy';

import { IntroPanel } from './IntroPanel';
import styles from './WizardPage.module.css';

/**
 * Wizard orchestration.
 *
 * Renders the sticky stepper and, beneath it, the screen for the current step.
 * Each screen is keyed by `step` so React remounts it on navigation, which
 * re-triggers the slide-in animation. The renew modal is always mounted and
 * self-hides based on store state.
 */
export function WizardPage() {
  const step = useSessionStore((s) => s.step);
  const vehicle = useSessionStore((s) => s.vehicle);
  const hasCoverage = useSessionStore((s) => s.selectedCoverageId !== null);
  const goBack = useSessionStore((s) => s.goBack);

  // The document and personal steps place "Volver" inline next to their CTA, so
  // only the checkout step needs the top-left back control here.
  const showTopBack = step === 'checkout';

  return (
    <>
      <Stepper />

      <main className={styles.stage}>
        {showTopBack && (
          <div className={styles.backRow}>
            <button type="button" className={styles.back} onClick={goBack}>
              ← Volver
            </button>
          </div>
        )}

        <div key={step} className={styles.panel}>
          {step === 'plate' && (
            <div className={styles.twoCol}>
              <IntroPanel />
              <PlateForm />
            </div>
          )}

          {step === 'document' && (
            <div className={styles.narrow}>
              <DocumentForm />
              {vehicle && <VehicleCard vehicle={vehicle} />}
            </div>
          )}

          {step === 'personal' && (
            <div className={styles.wide}>
              <PersonalDataForm />
            </div>
          )}

          {step === 'checkout' && (
            <div className={styles.payLayout}>
              <div>
                <CoverageSelector />
                {hasCoverage && <PaymentSection />}
              </div>
              <PurchaseSummary />
            </div>
          )}

          {step === 'result' && <EmissionResult />}
        </div>
      </main>

      <RenewPolicyModal />
    </>
  );
}
