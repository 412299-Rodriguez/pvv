import { useEffect, useState } from 'react';

import { useSessionStore } from '@/entities/session';
import { CoverageCard } from '@/entities/coverage';
import { getQuote, IngressError } from '@/shared/api/ingress';
import { Button, ShieldIcon, Spinner } from '@/shared/ui';
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

  /**
   * `unavailable` and `error` are different things and must not be worded the
   * same. A company with no price rule for this vehicle type will never quote
   * it, no matter how many times the buyer retries; a 500 might work next time.
   */
  const [status, setStatus] = useState<'loading' | 'ready' | 'unavailable' | 'error'>('loading');
  const [retry, setRetry] = useState(0);
  const track = useBIStore((s) => s.track);
  const goTo = useSessionStore((s) => s.goTo);

  useEffect(() => {
    let active = true;

    getQuote({ plate, documentType, documentNumber })
      .then((result) => {
        if (!active) return;
        setCoverages(result.coverages);
        // An empty list is a valid answer: the vehicle exists, this company just
        // has no price for it.
        setStatus(result.coverages.length > 0 ? 'ready' : 'unavailable');
        track('budget_calculated', { optionsCount: result.coverages.length });
      })
      .catch((e: unknown) => {
        if (!active) return;
        // The BFF answers 404 when the company has no products or pricing at
        // all — same dead end for the buyer as an empty list.
        const unavailable = e instanceof IngressError && e.statusCode === 404;
        setStatus(unavailable ? 'unavailable' : 'error');
        track('wizard_error', {
          step: 'quote',
          message: unavailable ? 'no coverage for vehicle' : 'quote failed',
        });
      });

    return () => {
      active = false;
    };
  }, [plate, documentType, documentNumber, setCoverages, track, retry]);

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
      {status === 'loading' ? (
        <div className={styles.loading}>
          <Spinner />
        </div>
      ) : status === 'unavailable' ? (
        <div className={styles.empty}>
          <div className={styles.emptyTitle}>No tenemos una cobertura para este vehículo</div>
          <p className={styles.emptyText}>
            Todavía no ofrecemos seguro para este tipo de vehículo o para su año. Podés probar
            con otra patente.
          </p>
          <Button variant="primary" onClick={() => goTo('plate')}>
            Probar con otra patente
          </Button>
        </div>
      ) : status === 'error' ? (
        <div className={styles.empty}>
          <div className={styles.emptyTitle}>No pudimos calcular tu cotización</div>
          <p className={styles.emptyText}>
            Hubo un problema al consultar los precios. Volvé a intentar en unos segundos.
          </p>
          <Button
            variant="primary"
            onClick={() => {
              setStatus('loading');
              setRetry((n) => n + 1);
            }}
          >
            Reintentar
          </Button>
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
