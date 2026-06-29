import styles from './Spinner.module.css';

interface SpinnerProps {
  /** Diameter in px. Default 56. */
  size?: number;
  className?: string;
}

/** Indeterminate circular spinner using the brand color. */
export function Spinner({ size = 56, className }: SpinnerProps) {
  return (
    <div
      className={[styles.spinner, className ?? ''].filter(Boolean).join(' ')}
      style={{ width: size, height: size }}
    >
      <svg viewBox="0 0 50 50">
        <circle cx="25" cy="25" r="20" fill="none" stroke="var(--color-border)" strokeWidth="5" />
        <path
          d="M25 5a20 20 0 0 1 20 20"
          fill="none"
          stroke="var(--color-primary)"
          strokeWidth="5"
          strokeLinecap="round"
        />
      </svg>
    </div>
  );
}
