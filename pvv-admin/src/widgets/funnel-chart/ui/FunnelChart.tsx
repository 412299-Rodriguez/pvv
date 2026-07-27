import { useState } from 'react'

import type { LeadFunnelStep } from '@/entities/lead'
import { FUNNEL_RAMP, VIZ, formatCount, formatPercent } from '@/shared/ui/viz'

interface FunnelChartProps {
  steps: LeadFunnelStep[]
  /** Everyone who opened the portal — the scale every bar is measured against. */
  totalLeads: number
}

/**
 * The conversion funnel as horizontal bars on one shared scale.
 *
 * Deliberately not trapezoids: with a trapezoid the reader compares areas, which
 * overstates the drop between stages. Bars share a baseline and a scale, so the
 * comparison is the length and nothing else.
 */
export function FunnelChart({ steps, totalLeads }: FunnelChartProps) {
  const [hovered, setHovered] = useState<number | null>(null)
  const [showTable, setShowTable] = useState(false)

  if (totalLeads === 0) {
    return (
      <p className="py-8 text-center text-sm text-stone-500">
        Todavía no hay visitas registradas en este período.
      </p>
    )
  }

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <div>
          <h2 className="text-base font-semibold text-stone-800">Embudo de conversión</h2>
          <p className="text-sm text-stone-500">
            De {formatCount(totalLeads)} visitas al portal, cuántas llegaron a cada paso
          </p>
        </div>
        <button
          type="button"
          onClick={() => setShowTable((current) => !current)}
          className="rounded-lg border border-stone-300 px-3 py-1.5 text-xs font-semibold text-stone-600 transition hover:bg-stone-50"
        >
          {showTable ? 'Ver gráfico' : 'Ver tabla'}
        </button>
      </div>

      {showTable ? (
        <FunnelTable steps={steps} totalLeads={totalLeads} />
      ) : (
        // 2px of surface between touching bars — white does the separating, not a
        // stroke around each mark.
        <div className="flex flex-col" style={{ gap: '2px' }}>
          {steps.map((step, index) => {
            const share = (step.reached / totalLeads) * 100
            // Everyone who reached the previous rung but not this one.
            const previousReached = index === 0 ? totalLeads : (steps[index - 1]?.reached ?? totalLeads)
            const dropped = previousReached - step.reached

            return (
              <div
                key={step.step}
                className="relative grid grid-cols-[9rem_1fr_5.5rem] items-center gap-3 rounded-lg py-1.5 transition hover:bg-stone-50"
                onMouseEnter={() => setHovered(step.step)}
                onMouseLeave={() => setHovered(null)}
              >
                <div className="text-sm font-medium text-stone-700">
                  {step.step}. {step.label}
                </div>

                <div className="h-5 w-full rounded-sm" style={{ backgroundColor: VIZ.track }}>
                  <div
                    // Square where it meets the baseline, 4px rounded at the data end.
                    className="h-5 rounded-l-sm rounded-r"
                    style={{
                      width: `${Math.max(share, 0.8)}%`,
                      backgroundColor: FUNNEL_RAMP[index] ?? FUNNEL_RAMP[FUNNEL_RAMP.length - 1],
                    }}
                  />
                </div>

                {/* Value outside the bar end, so a short bar never clips its label. */}
                <div className="text-right text-sm text-stone-700">
                  <span className="font-semibold">{formatCount(step.reached)}</span>
                  <span className="ml-1 text-xs text-stone-500">{formatPercent(share)}</span>
                </div>

                {hovered === step.step ? (
                  <div className="pointer-events-none absolute left-36 top-full z-10 mt-1 rounded-lg border border-stone-200 bg-white px-3 py-2 text-xs shadow-lg">
                    <div className="font-semibold text-stone-800">{step.label}</div>
                    <div className="mt-1 text-stone-600">
                      {formatCount(step.reached)} de {formatCount(totalLeads)} visitas
                    </div>
                    <div className="text-stone-600">
                      {formatPercent(step.conversionFromPrevious)} del paso anterior
                    </div>
                    {dropped > 0 ? (
                      <div className="text-stone-500">Se fueron acá: {formatCount(dropped)}</div>
                    ) : null}
                  </div>
                ) : null}
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}

/** The WCAG-clean twin of the chart — every value reachable without color or hover. */
function FunnelTable({ steps, totalLeads }: FunnelChartProps) {
  return (
    <table className="w-full text-sm">
      <thead>
        <tr className="border-b border-stone-200 text-left text-xs uppercase tracking-wide text-stone-500">
          <th className="py-2 font-semibold">Paso</th>
          <th className="py-2 text-right font-semibold">Llegaron</th>
          <th className="py-2 text-right font-semibold">% del total</th>
          <th className="py-2 text-right font-semibold">% del paso anterior</th>
        </tr>
      </thead>
      <tbody className="tabular-nums">
        {steps.map((step) => (
          <tr key={step.step} className="border-b border-stone-100">
            <td className="py-2 text-stone-700">
              {step.step}. {step.label}
            </td>
            <td className="py-2 text-right text-stone-700">{formatCount(step.reached)}</td>
            <td className="py-2 text-right text-stone-600">
              {formatPercent((step.reached / totalLeads) * 100)}
            </td>
            <td className="py-2 text-right text-stone-600">
              {formatPercent(step.conversionFromPrevious)}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}
