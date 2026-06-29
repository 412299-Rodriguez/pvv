import { useSessionStore } from '@/entities/session';
import { availableCoverages, CoverageCard } from '@/entities/coverage';
import { ShieldIcon } from '@/shared/ui';
import styles from './CoverageSelector.module.css';

/**
 * Step 4 (part 1) — coverage selection grid.
 * Selecting a coverage updates the store, which reveals the payment section and
 * fills the purchase summary sidebar.
 */
export function CoverageSelector() {
  const selectedId = useSessionStore((s) => s.selectedCoverageId);
  const selectCoverage = useSessionStore((s) => s.selectCoverage);

  return (
    <section>
      <h2 className={styles.secTitle}>
        <ShieldIcon />
        Elegí tu cobertura
      </h2>
      <div className={styles.grid}>
        {availableCoverages.map((coverage) => (
          <CoverageCard
            key={coverage.id}
            coverage={coverage}
            selected={coverage.id === selectedId}
            onSelect={selectCoverage}
          />
        ))}
      </div>
    </section>
  );
}
