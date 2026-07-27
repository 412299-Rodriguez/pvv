import { useEffect, useState } from 'react'

import { getLeadFunnel, type LeadFilters, type LeadFunnel } from '@/entities/lead'
import { Card, RefreshButton, StatTile } from '@/shared/ui'
import { formatCount, formatPercent } from '@/shared/ui/viz'
import { presetToFilter, type RangePreset } from '@/shared/lib'
import { DateRangeFilter } from '@/widgets/date-range-filter'
import { FunnelChart } from '@/widgets/funnel-chart'

/** The dashboard reloads itself so a screen left open does not go stale. */
const REFRESH_MS = 5 * 60 * 1000

/**
 * Conversion funnel and headline numbers for the operator's own portal. Which
 * company that is comes from the token, so there is nothing to pass in.
 */
export function LeadDashboard() {
  const [preset, setPreset] = useState<RangePreset>('30d')
  const [error, setError] = useState<string | null>(null)
  // Bumped by the refresh button; part of the key so a manual reload dims the
  // numbers exactly like a filter change does.
  const [reload, setReload] = useState(0)

  // The loaded slice is tagged with the filter it belongs to, so a pending
  // change shows the previous numbers dimmed instead of a skeleton flash.
  const filterKey = `${preset}|${reload}`
  const [loaded, setLoaded] = useState<{ key: string; funnel: LeadFunnel } | null>(null)

  useEffect(() => {
    let active = true

    const load = () => {
      const filters: LeadFilters = presetToFilter(preset)

      getLeadFunnel(filters)
        .then((funnel) => {
          if (!active) return
          setLoaded({ key: filterKey, funnel })
          setError(null)
        })
        .catch(() => {
          if (active) setError('No pudimos cargar las métricas.')
        })
    }

    load()
    const timer = window.setInterval(load, REFRESH_MS)
    return () => {
      active = false
      window.clearInterval(timer)
    }
  }, [filterKey, preset, reload])

  const funnel = loaded?.funnel ?? null
  const stale = loaded !== null && loaded.key !== filterKey

  return (
    <div>
      <DateRangeFilter value={preset} onChange={setPreset}>
        <RefreshButton onClick={() => setReload((n) => n + 1)} busy={stale} />
      </DateRangeFilter>

      {error ? (
        <Card className="border-red-200 bg-red-50 text-sm text-red-700">{error}</Card>
      ) : null}

      {funnel === null ? (
        <p className="py-12 text-center text-sm text-slate-500">Cargando métricas…</p>
      ) : (
        <div className={stale ? 'opacity-60 transition-opacity' : 'transition-opacity'}>
          <div className="mb-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <StatTile
              label="Visitas al portal"
              value={formatCount(funnel.totalLeads)}
              hint="Intentos de compra iniciados"
            />
            <StatTile label="Pólizas emitidas" value={formatCount(funnel.policiesIssued)} />
            <StatTile
              label="Conversión global"
              value={formatPercent(funnel.overallConversion)}
              hint="De visita a póliza"
            />
            <StatTile
              label="Abandonados"
              value={formatCount(funnel.abandoned)}
              hint={`${formatCount(funnel.active)} todavía en curso`}
            />
          </div>

          <Card>
            <FunnelChart steps={funnel.steps} totalLeads={funnel.totalLeads} />
          </Card>
        </div>
      )}
    </div>
  )
}
