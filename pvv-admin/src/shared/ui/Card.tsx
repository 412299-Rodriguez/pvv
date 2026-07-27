import type { ReactNode } from 'react'

/**
 * A surface. `padded` is turned off when the card holds its own sections — a
 * tab strip or a table has to reach the edges to read as part of the card
 * rather than as something floating inside it.
 */
export function Card({
  children,
  className = '',
  padded = true,
}: {
  children: ReactNode
  className?: string
  padded?: boolean
}) {
  return (
    <div
      className={`rounded-xl border border-slate-200 bg-white shadow-[0_1px_2px_rgba(15,23,42,0.04)] ${
        padded ? 'p-6' : ''
      } ${className}`}
    >
      {children}
    </div>
  )
}
