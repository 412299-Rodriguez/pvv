import type { InputHTMLAttributes, ReactNode } from 'react';
import styles from './TextField.module.css';

interface TextFieldProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string;
  /** Validation error message; when set the field renders its error state. */
  error?: string | undefined;
  /** Static prefix rendered inside the input, e.g. "+54". */
  prefix?: string;
  /** Trailing adornment, typically an icon. */
  adornment?: ReactNode;
  /** Highlight as auto-filled from a found contact. */
  prefilled?: boolean;
}

/**
 * Labelled text input with optional prefix, trailing icon, prefilled highlight
 * and inline error message.
 */
export function TextField({
  label,
  error,
  prefix,
  adornment,
  prefilled = false,
  className,
  id,
  ...rest
}: TextFieldProps) {
  const inputClasses = [
    styles.input,
    prefix ? styles.hasPrefix : '',
    adornment ? styles.hasAdornment : '',
    prefilled ? styles.prefilled : '',
    error ? styles.error : '',
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <div className={[styles.field, className ?? ''].filter(Boolean).join(' ')}>
      <label className={styles.label} htmlFor={id}>
        {label}
      </label>
      <div className={styles.inputWrap}>
        {prefix && <span className={styles.prefix}>{prefix}</span>}
        <input id={id} className={inputClasses} {...rest} />
        {adornment && <span className={styles.adornment}>{adornment}</span>}
      </div>
      {error && <p className={styles.errorMsg}>{error}</p>}
    </div>
  );
}
