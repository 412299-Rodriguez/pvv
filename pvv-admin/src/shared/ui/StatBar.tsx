import type { ReactNode } from 'react'

export interface Stat {
  label: string
  value: ReactNode
  hint?: string
}

/**
 * A row of headline numbers as one panel split by hairlines, rather than four
 * separate floating cards.
 *
 * They are readings of the same thing, so they belong in the same frame — and
 * dividing instead of boxing removes the empty corner a card gets when it has
 * no caption. No icons: a number with a label does not need a picture of a
 * person next to it, and four tinted glyphs are the fastest way to make a
 * screen look like a template.
 */
export function StatBar({ stats }: { stats: Stat[] }) {
  return (
    <div className="grid divide-y divide-stone-200 overflow-hidden rounded-lg border border-stone-200 bg-white lg:grid-cols-4 lg:divide-x lg:divide-y-0">
      {stats.map((stat) => (
        <div key={stat.label} className="px-5 py-4">
          <div className="text-xs font-medium text-stone-500">{stat.label}</div>
          <div className="mt-1.5 text-[26px] font-semibold leading-none tracking-tight text-stone-900">
            {stat.value}
          </div>
          {stat.hint ? <div className="mt-1.5 text-xs text-stone-400">{stat.hint}</div> : null}
        </div>
      ))}
    </div>
  )
}
