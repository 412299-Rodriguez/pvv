import styles from './SegmentedControl.module.css';

interface SegmentedControlProps<T extends string> {
  options: readonly T[];
  value: T;
  onChange: (value: T) => void;
  ariaLabel?: string;
}

/**
 * Generic segmented (tab-like) selector. Used for the document-type picker
 * (DNI / CUIL / CUIT) but reusable for any short option set.
 */
export function SegmentedControl<T extends string>({
  options,
  value,
  onChange,
  ariaLabel,
}: SegmentedControlProps<T>) {
  return (
    <div className={styles.group} role="tablist" aria-label={ariaLabel}>
      {options.map((option) => (
        <button
          key={option}
          type="button"
          role="tab"
          aria-selected={option === value}
          className={[styles.segment, option === value ? styles.active : '']
            .filter(Boolean)
            .join(' ')}
          onClick={() => onChange(option)}
        >
          {option}
        </button>
      ))}
    </div>
  );
}
