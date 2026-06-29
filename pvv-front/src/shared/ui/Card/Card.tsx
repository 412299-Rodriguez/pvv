import type { HTMLAttributes, ReactNode } from 'react';
import styles from './Card.module.css';

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
}

/** White surface container with the standard border, radius and elevation. */
export function Card({ className, children, ...rest }: CardProps) {
  return (
    <div className={[styles.card, className ?? ''].filter(Boolean).join(' ')} {...rest}>
      {children}
    </div>
  );
}
