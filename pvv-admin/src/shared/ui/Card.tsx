import type { ReactNode } from 'react'

/**
 * A surface. Hairline, no shadow — a panel of flat white planes on warm paper
 * separates by edge and spacing, and a drop shadow under everything is the
 * fastest way to make a screen look like a stack of floating widgets.
 *
 * `padded` is turned off when the card holds its own sections: a tab strip or a
 * table has to reach the edges to read as part of the card.
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
      className={`rounded-lg border border-stone-200 bg-white ${padded ? 'p-6' : ''} ${className}`}
    >
      {children}
    </div>
  )
}
