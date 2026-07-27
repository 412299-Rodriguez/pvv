import type { ButtonHTMLAttributes } from 'react'

type ButtonVariant = 'primary' | 'secondary' | 'danger' | 'ghost'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant
}

/**
 * Ink, not blue. The primary action is the darkest thing on the screen, which
 * makes it the most prominent without spending a colour on it — colour in this
 * panel is reserved for state (sold, abandoned, rejected).
 */
const VARIANTS: Record<ButtonVariant, string> = {
  primary: 'bg-stone-900 text-white hover:bg-stone-700',
  secondary: 'border border-stone-300 bg-white text-stone-700 hover:border-stone-400',
  danger: 'border border-stone-300 bg-white text-red-600 hover:border-red-300 hover:bg-red-50',
  ghost: 'text-stone-600 hover:bg-stone-100',
}

export function Button({ variant = 'primary', className = '', ...rest }: ButtonProps) {
  return (
    <button
      className={`rounded-md px-3.5 py-2 text-sm font-medium transition disabled:cursor-not-allowed disabled:opacity-40 ${VARIANTS[variant]} ${className}`}
      {...rest}
    />
  )
}
