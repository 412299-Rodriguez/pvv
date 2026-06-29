import { ShieldIcon, CheckIcon } from '@/shared/ui/icons';
import { Button } from '@/shared/ui/Button';
import { formatCurrency } from '@/shared/lib';
import type { Coverage } from '../model/types';
import styles from './CoverageCard.module.css';

interface CoverageCardProps {
  coverage: Coverage;
  selected: boolean;
  onSelect: (id: string) => void;
}

/**
 * Product card for a single coverage. Clicking anywhere on the card (or its
 * button) selects it; the selected state recolors the header and swaps the
 * button label.
 */
export function CoverageCard({ coverage, selected, onSelect }: CoverageCardProps) {
  const classes = [styles.card, selected ? styles.selected : '']
    .filter(Boolean)
    .join(' ');

  return (
    <div className={classes} onClick={() => onSelect(coverage.id)}>
      {coverage.recommended && <span className={styles.badge}>Más elegido</span>}

      <div className={styles.head}>
        <span className={styles.headCheck} aria-hidden>
          ✓
        </span>
        <div className={styles.name}>{coverage.name}</div>
        <div className={styles.type}>{coverage.coverageType}</div>
      </div>

      <div className={styles.body}>
        <div className={styles.shield}>
          <ShieldIcon />
        </div>

        <div className={styles.price}>
          <span className={styles.amount}>{formatCurrency(coverage.pricePerYear)}</span>
          <span className={styles.per}>/año</span>
        </div>

        <div className={styles.benefits}>
          {coverage.benefits.map((benefit) => (
            <div key={benefit} className={styles.benefit}>
              <CheckIcon />
              {benefit}
            </div>
          ))}
        </div>

        <Button
          variant="primary"
          fullWidth
          className={styles.selectBtn}
          onClick={(e) => {
            e.stopPropagation();
            onSelect(coverage.id);
          }}
        >
          {selected ? '✓ Seleccionada' : 'Seleccionar'}
        </Button>
      </div>
    </div>
  );
}
