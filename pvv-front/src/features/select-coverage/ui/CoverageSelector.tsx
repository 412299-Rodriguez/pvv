import { useEffect, useState } from 'react';

import { useSessionStore } from '@/entities/session';
import { CoverageCard } from '@/entities/coverage';
import { getQuote } from '@/shared/api/ingress';
import { ShieldIcon, Spinner } from '@/shared/ui';
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

  useEffect(() => {
    let active = true;
    setLoading(true);
    getQuote({ plate, documentType, documentNumber }).then((result) => {
      if (!active) return;
      setCoverages(result.coverages);
      setLoading(false);
    });
    return () => {
      active = false;
    };
  }, [plate, documentType, documentNumber, setCoverages]);

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
              onSelect={selectCoverage}
            />
          ))}
        </div>
      )}
    </section>
  );
}
