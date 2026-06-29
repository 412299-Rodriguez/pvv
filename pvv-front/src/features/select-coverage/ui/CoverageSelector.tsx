import { useEffect, useState } from 'react';

import { useSessionStore } from '@/entities/session';
import { CoverageCard, type Coverage } from '@/entities/coverage';
import { getQuote } from '@/shared/api/ingress';
import { ShieldIcon, Spinner } from '@/shared/ui';
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
  const plate = useSessionStore((s) => s.plate);
  const documentType = useSessionStore((s) => s.documentType);
  const documentNumber = useSessionStore((s) => s.documentNumber);

  const [coverages, setCoverages] = useState<Coverage[]>([]);
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
  }, [plate, documentType, documentNumber]);

  return (
    <section>
      <h2 className={styles.secTitle}>
        <ShieldIcon />
        Elegí tu cobertura
      </h2>
      {loading ? (
        <Spinner />
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
