import type { ReactNode } from 'react'

/**
 * A headline number. Used instead of a one-bar chart: when the data is a single
 * current value, the number IS the chart.
 *
 * Values use the font's proportional figures on purpose — tabular-nums makes a
 * large standalone number look loose, and is kept for columns that align.
 */
export function StatTile({
  label,
  value,
  hint,
}: {
  label: string
  value: ReactNode
  hint?: string
}) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white p-5">
      <div className="text-sm font-medium text-slate-500">{label}</div>
      <div className="mt-1 text-3xl font-semibold text-slate-900">{value}</div>
      {hint ? <div className="mt-1 text-xs text-slate-500">{hint}</div> : null}
    </div>
  )
}
