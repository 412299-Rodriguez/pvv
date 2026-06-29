import type { ButtonHTMLAttributes, ReactNode } from 'react';
import styles from './Button.module.css';

export type ButtonVariant = 'primary' | 'outline' | 'ghostBorder' | 'mercadoPago';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  /** Stretch to the full width of the container. */
  fullWidth?: boolean;
  children: ReactNode;
}

/**
 * App button. Visual variants:
 *  - primary      solid brand color (default CTA)
 *  - outline      bordered, used as secondary action
 *  - ghostBorder  muted bordered, used for "cancel"
 *  - mercadoPago  gradient brand->MP-blue, the final pay action
 */
export function Button({
  variant = 'primary',
  fullWidth = false,
  className,
  children,
  ...rest
}: ButtonProps) {
  const classes = [
    styles.btn,
    styles[variant],
    fullWidth ? styles.fullWidth : '',
    className ?? '',
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <button className={classes} {...rest}>
      {children}
    </button>
  );
}
