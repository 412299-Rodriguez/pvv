import type { ReactNode } from 'react';
import { CheckIcon } from '@/shared/ui/icons';
import styles from './Checkbox.module.css';

interface CheckboxProps {
  checked: boolean;
  onChange: (checked: boolean) => void;
  children: ReactNode;
}

/** Custom-styled checkbox with a label. The whole row is clickable. */
export function Checkbox({ checked, onChange, children }: CheckboxProps) {
  return (
    <div
      className={[styles.row, checked ? styles.checked : ''].filter(Boolean).join(' ')}
      role="checkbox"
      aria-checked={checked}
      tabIndex={0}
      onClick={() => onChange(!checked)}
      onKeyDown={(e) => {
        if (e.key === ' ' || e.key === 'Enter') {
          e.preventDefault();
          onChange(!checked);
        }
      }}
    >
      <span className={styles.box}>
        <CheckIcon className={styles.tick} />
      </span>
      <span className={styles.text}>{children}</span>
    </div>
  );
}
