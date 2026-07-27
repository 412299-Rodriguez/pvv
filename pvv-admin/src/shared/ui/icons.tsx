import type { SVGProps } from 'react'

/**
 * Line icons for the stat tiles. Stroked rather than filled so they stay quiet
 * next to the number, which is the thing being read.
 */
const base = {
  viewBox: '0 0 24 24',
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 1.8,
  strokeLinecap: 'round',
  strokeLinejoin: 'round',
  'aria-hidden': true,
} as const

export function UsersIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg {...base} {...props}>
      <path d="M16 19v-1.5a3.5 3.5 0 0 0-3.5-3.5h-5A3.5 3.5 0 0 0 4 17.5V19" />
      <circle cx="10" cy="8" r="3.2" />
      <path d="M20 19v-1.4a3.5 3.5 0 0 0-2.6-3.4M15.4 5.2a3.2 3.2 0 0 1 0 5.6" />
    </svg>
  )
}

/** Someone who came in and turned around. */
export function DoorIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg {...base} {...props}>
      <path d="M9 21H5a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1h4" />
      <path d="M16 16l4-4-4-4M20 12H9" />
    </svg>
  )
}

/** A finished purchase. */
export function CheckBadgeIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg {...base} {...props}>
      <path d="M12 3l2.1 1.6 2.6-.2.9 2.5 2.2 1.4-.9 2.5.9 2.5-2.2 1.4-.9 2.5-2.6-.2L12 21l-2.1-1.6-2.6.2-.9-2.5L4.2 15.7l.9-2.5-.9-2.5 2.2-1.4.9-2.5 2.6.2z" />
      <path d="M9 12.2l2.1 2.1L15.3 10" />
    </svg>
  )
}

export function TrendingUpIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg {...base} {...props}>
      <path d="M3 17l6-6 4 4 7-7" />
      <path d="M14 8h6v6" />
    </svg>
  )
}
