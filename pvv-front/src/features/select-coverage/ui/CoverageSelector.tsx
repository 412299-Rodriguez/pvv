import { useEffect, useState } from 'react';

import { useSessionStore } from '@/entities/session';
import { CoverageCard } from '@/entities/coverage';
import { getQuote } from '@/shared/api/ingress';
import { ShieldIcon, Spinner } from '@/shared/ui';
import { useAlertModalStore } from '@/shared/lib';
import { useBIStore } from '@/shared/analytics';
import { useText } from '@/entities/company';
import styles from './CoverageSelector.module.css';

/**
 * Step 4 (part 1) — coverage selection grid.
 * Quotes available coverages through the ingress client (mock for now), then
 * selecting one updates the store, which reveals the payment section and fills
 * the purchase summary sidebar.
 */
export function CoverageSelector() {
  const selectedId = useSessionStore((s) => s.selectedCoverageId);
  const selectCoverage = useSessionStore((s) => s.selectCoverage);
  const coverages = useSessionStore((s) => s.coverages);
  const setCoverages = useSessionStore((s) => s.setCoverages);
  const plate = useSessionStore((s) => s.plate);
  const documentType = useSessionStore((s) => s.documentType);
  const documentNumber = useSessionStore((s) => s.documentNumber);

  const [loading, setLoading] = useState(true);
  const track = useBIStore((s) => s.track);
  const showAlert = useAlertModalStore((s) => s.showAlert);

  useEffect(() => {
    let active = true;
    setLoading(true);
    getQuote({ plate, documentType, documentNumber })
      .then((result) => {
        if (!active) return;
        setCoverages(result.coverages);
        setLoading(false);
        track('budget_calculated', { optionsCount: result.coverages.length });
      })
      .catch(() => {
        // Without this the spinner would spin forever on a failed quote.
        if (!active) return;
        setLoading(false);
        track('wizard_error', { step: 'quote', message: 'quote failed' });
        showAlert(
          'No pudimos calcular tu cotización',
          'Ocurrió un problema. Intentá de nuevo en unos segundos.',
        );
      });
    return () => {
      active = false;
    };
  }, [plate, documentType, documentNumber, setCoverages, track, showAlert]);

  // Funnel step 3 completes when a coverage is picked, not when it is shown.
  const handleSelect = (coverageId: string) => {
    const coverage = coverages.find((c) => c.id === coverageId);
    track('product_selected', {
      productId: coverageId,
      productName: coverage?.name,
      amount: coverage?.pricePerYear,
    });
    selectCoverage(coverageId);
  };

  return (
    <section>
      <h2 className={styles.secTitle}>
        <ShieldIcon />
        {useText('coverageTitle', 'Elegí tu cobertura')}
      </h2>
      {loading ? (
        <div className={styles.loading}>
          <Spinner />
        </div>
      ) : (
        <div className={styles.grid}>
          {coverages.map((coverage) => (
            <CoverageCard
              key={coverage.id}
              coverage={coverage}
              selected={coverage.id === selectedId}
              onSelect={handleSelect}
            />
          ))}
        </div>
      )}
    </section>
  );
}
