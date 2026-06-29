import { useSessionStore, selectSelectedCoverage } from '@/entities/session';
import { demoVehicle, vehicleTitle } from '@/entities/vehicle';
import { demoDocumentNumber, demoPolicyholder, policyholderFullName } from '@/entities/policyholder';
import { CarIcon, UserIcon, BoltFilledIcon } from '@/shared/ui/icons';
import { formatCurrency, formatDocument } from '@/shared/lib';
import styles from './PurchaseSummary.module.css';

/**
 * Sticky checkout sidebar: vehicle, holder, the selected coverage and the total.
 * Reactively reflects whatever the user has chosen so far.
 */
export function PurchaseSummary() {
  const policyholder = useSessionStore((s) => s.policyholder);
  const documentType = useSessionStore((s) => s.documentType);
  const documentNumber = useSessionStore((s) => s.documentNumber);
  const coverage = useSessionStore(selectSelectedCoverage);

  const holderName = policyholderFullName(policyholder) || policyholderFullName(demoPolicyholder);
  const docValue = documentNumber || demoDocumentNumber;

  return (
    <aside className={styles.summary}>
      <div className={styles.card}>
        <h3 className={styles.heading}>Resumen de tu compra</h3>

        <div className={styles.row}>
          <div className={styles.icon}>
            <CarIcon />
          </div>
          <div>
            <div className={styles.label}>Tu vehículo</div>
            <div className={styles.value}>
              {vehicleTitle(demoVehicle)}
              <small>Patente {demoVehicle.plate}</small>
            </div>
          </div>
        </div>

        <div className={styles.divider} />

        <div className={styles.row}>
          <div className={styles.icon}>
            <UserIcon />
          </div>
          <div>
            <div className={styles.label}>Titular</div>
            <div className={styles.value}>
              {holderName}
              <small>
                {documentType} {formatDocument(docValue)}
              </small>
            </div>
          </div>
        </div>

        <div className={styles.divider} />

        <div className={styles.productLabel}>Producto seleccionado</div>
        {coverage ? (
          <div className={styles.product}>
            {coverage.name} · {coverage.coverageType}
          </div>
        ) : (
          <div className={`${styles.product} ${styles.empty}`}>Elegí una cobertura</div>
        )}

        <div className={styles.totalLabel}>Total a pagar</div>
        <div className={styles.total}>
          {coverage ? (
            <>
              {formatCurrency(coverage.pricePerYear)}
              <span className={styles.per}> /año</span>
            </>
          ) : null}
        </div>

        <div className={styles.emit}>
          <BoltFilledIcon />
          Emisión inmediata tras el pago
        </div>
      </div>
    </aside>
  );
}
