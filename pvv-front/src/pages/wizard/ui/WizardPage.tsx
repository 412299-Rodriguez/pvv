import { useSessionStore } from '@/entities/session';
import { usePvvConfigStore } from '@/entities/company';
import { VehicleCard } from '@/entities/vehicle';
import { useInfoModalStore } from '@/shared/lib';

import { Stepper } from '@/widgets/stepper';
import { PurchaseSummary } from '@/widgets/purchase-summary';
import { AdCarousel } from '@/widgets/ad-carousel';
import { InfoModals } from '@/widgets/info-modals';

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
 * Wizard orchestration: a per-company brand header, the sticky stepper, and the
 * screen for the current step. Step 1 lays out the intro, the plate form and the
 * company's ad carousel.
 */
export function WizardPage() {
  const step = useSessionStore((s) => s.step);
  const vehicle = useSessionStore((s) => s.vehicle);
  const hasCoverage = useSessionStore((s) => s.selectedCoverageId !== null);
  const goBack = useSessionStore((s) => s.goBack);

  const adImages = usePvvConfigStore((s) => s.adImages);
  const footerText = usePvvConfigStore((s) => s.footerText);
  const openModal = useInfoModalStore((s) => s.openModal);

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
            <div className={styles.step1}>
              <div className={styles.introCol}>
                <IntroPanel />
              </div>
              <div className={styles.rightCol}>
                <PlateForm />
                {adImages.length > 0 && <AdCarousel images={adImages} />}
              </div>
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

      <footer className={styles.footer}>
        <button type="button" className={styles.faqLink} onClick={() => openModal('faq')}>
          Preguntas frecuentes
        </button>
        {footerText && <span className={styles.footerText}>{footerText}</span>}
      </footer>

      <RenewPolicyModal />
      <InfoModals />
    </>
  );
}
