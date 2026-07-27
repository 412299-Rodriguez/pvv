import type { ReactNode } from 'react'

type Tone = 'neutral' | 'success'

const TONES: Record<Tone, string> = {
  neutral: 'bg-slate-100 text-slate-500',
  // Matches the "Compró" badge in the leads table, so the same colour keeps
  // meaning the same thing across the screen.
  success: 'bg-emerald-50 text-emerald-600',
}

/**
 * A headline number. Used instead of a one-bar chart: when the data is a single
 * current value, the number IS the chart.
 *
 * The tile stretches to the row height and pins its caption to the bottom, so a
 * row of them lines up even when only some have one. Values use the font's
 * proportional figures on purpose — tabular-nums makes a large standalone
 * number look loose, and is kept for columns that align.
 */
export function StatTile({
  label,
  value,
  hint,
  icon,
  tone = 'neutral',
}: {
  label: string
  value: ReactNode
  hint?: string
  icon?: ReactNode
  tone?: Tone
}) {
  return (
    <div className="flex h-full flex-col rounded-xl border border-slate-200 bg-white p-5 shadow-[0_1px_2px_rgba(15,23,42,0.04)]">
      <div className="flex items-start justify-between gap-3">
        <span className="text-xs font-semibold uppercase tracking-wide text-slate-500">
          {label}
        </span>
        {icon ? (
          <span className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-lg ${TONES[tone]}`}>
            {icon}
          </span>
        ) : null}
      </div>

      <div className="mt-3 text-3xl font-semibold leading-none text-slate-900">{value}</div>

      {/* Non-breaking space keeps the tiles the same height without a caption. */}
      <div className="mt-auto pt-2 text-xs text-slate-500">{hint ?? ' '}</div>
    </div>
  )
}
