import { useEffect, useState } from 'react'

import {
  FUNNEL_STEPS,
  getLeadFunnel,
  listLeads,
  type LeadFilters,
  type LeadFunnel,
  type LeadListItem,
  type LeadStatus,
  type PagedLeads,
} from '@/entities/lead'
import { useSessionStore } from '@/entities/session'
import { RecoveryModal } from '@/features/lead-recovery'
import { Card, RefreshButton, StatTile } from '@/shared/ui'
import { formatCount, formatPercent } from '@/shared/ui/viz'
import { portalUrl, presetToFilter, type RangePreset } from '@/shared/lib'
import { DateRangeFilter } from '@/widgets/date-range-filter'
import { LeadsTable } from '@/widgets/leads-table'

import { exportLeadsCsv } from '../lib/exportLeads'

const PAGE_SIZE = 20

/**
 * One tab per step of the funnel a journey can stall on.
 *
 * The two ends are deliberately absent, because neither is a step. Reaching
 * step 5 means the policy was issued, so those leads never got stuck — they are
 * the sale. And a visitor who opened the portal without searching a plate never
 * entered the funnel at all. Both are counted in the cards above: there is
 * nothing to act on in either list (no contact details, or nothing left to do).
 */
const STOP_TABS = FUNNEL_STEPS.filter((s) => s.step < 5).map((s) => ({
  step: s.step,
  label: `Paso ${s.step} · ${s.label}`,
}))

/** Visitors who never started: they have no data on them beyond having arrived. */
const NEVER_STARTED_STEP = 0

/** Inside a tab, the only two outcomes possible: still open, or given up on. */
const OUTCOME_FILTERS: { value: Extract<LeadStatus, 'abandoned' | 'active'>; label: string }[] = [
  { value: 'abandoned', label: 'Abandonaron' },
  { value: 'active', label: 'En curso' },
]

interface LeadsExplorerProps {
  /** Signs the recovery email; the shell loads it once per session. */
  companyName?: string
}

/** The leads table, grouped by where each purchase attempt stopped. */
export function LeadsExplorer({ companyName }: LeadsExplorerProps) {
  const [preset, setPreset] = useState<RangePreset>('30d')
  // Opens on the first real milestone; the counts on each tab say where to look.
  const [step, setStep] = useState(1)
  const [outcome, setOutcome] = useState<'abandoned' | 'active'>('abandoned')
  const [page, setPage] = useState(1)
  const [error, setError] = useState<string | null>(null)
  const [exporting, setExporting] = useState(false)
  const [recovering, setRecovering] = useState<LeadListItem | null>(null)
  // Bumped by the refresh button; reloads both the table and the counters.
  const [reload, setReload] = useState(0)

  // Used only to build the portal link inside a recovery email.
  const sessionToken = useSessionStore((s) => s.companyToken)

  const filterKey = `${preset}|${step}|${outcome}|${page}|${reload}`
  const [loaded, setLoaded] = useState<{ key: string; data: PagedLeads } | null>(null)

  // Counts for the tabs and the sale card. Only the date range moves them, so
  // this reloads far less often than the table.
  const [funnel, setFunnel] = useState<LeadFunnel | null>(null)

  const currentFilters = (): LeadFilters => ({
    ...presetToFilter(preset),
    lastStep: step,
    status: outcome,
  })

  useEffect(() => {
    let active = true

    listLeads(currentFilters(), page, PAGE_SIZE)
      .then((data) => {
        if (!active) return
        setLoaded({ key: filterKey, data })
        setError(null)
      })
      .catch(() => {
        if (active) setError('No pudimos cargar los leads.')
      })

    return () => {
      active = false
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filterKey])

  useEffect(() => {
    let active = true

    getLeadFunnel(presetToFilter(preset))
      .then((data) => active && setFunnel(data))
      .catch(() => {})

    return () => {
      active = false
    }
  }, [preset, reload])

  const changeFilter = (apply: () => void) => {
    apply()
    setPage(1)
  }

  /** Exports what is on screen — this tab, this outcome, every page of it. */
  const handleExport = async () => {
    setExporting(true)
    setError(null)
    try {
      const result = await exportLeadsCsv(currentFilters())
      if (result.truncated) {
        setError(`El archivo trae las primeras ${formatCount(result.rows)} filas; hay más.`)
      }
    } catch {
      setError('No pudimos generar el archivo.')
    } finally {
      setExporting(false)
    }
  }

  const breakdown = (forStep: number) => funnel?.stoppedAt.find((s) => s.step === forStep)

  const data = loaded?.data ?? null
  const stale = loaded !== null && loaded.key !== filterKey

  const total = data?.total ?? 0
  const firstRow = total === 0 ? 0 : (page - 1) * PAGE_SIZE + 1
  const lastRow = Math.min(page * PAGE_SIZE, total)
  const hasNext = page * PAGE_SIZE < total

  return (
    <div>
      {/*
        The universe first, then the two ends of the journey. Neither end is a
        step, which is why they are numbers here and not tabs below.
      */}
      <div className="mb-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatTile label="Total de leads" value={formatCount(funnel?.totalLeads ?? 0)} />
        <StatTile
          label="Solo entraron"
          value={formatCount(breakdown(NEVER_STARTED_STEP)?.total ?? 0)}
          hint="No llegaron a buscar un vehículo"
        />
        <StatTile
          label="Terminaron comprando"
          value={formatCount(funnel?.completed ?? 0)}
          hint="Con la póliza emitida"
        />
        <StatTile
          label="Conversión"
          value={formatPercent(funnel?.overallConversion ?? 0)}
          hint="Del portal a la póliza"
        />
      </div>

      <DateRangeFilter value={preset} onChange={(next) => changeFilter(() => setPreset(next))}>
        <RefreshButton onClick={() => setReload((n) => n + 1)} busy={stale} />
      </DateRangeFilter>

      {/* Where the journey ended. */}
      <div className="mb-3 flex flex-wrap gap-1 border-b border-slate-200">
        {STOP_TABS.map((tab) => {
          const count = breakdown(tab.step)?.total ?? 0
          return (
            <button
              key={tab.step}
              type="button"
              onClick={() => changeFilter(() => setStep(tab.step))}
              className={`-mb-px flex items-center gap-2 border-b-2 px-3 py-2 text-sm font-semibold transition ${
                step === tab.step
                  ? 'border-blue-600 text-blue-700'
                  : 'border-transparent text-slate-500 hover:text-slate-700'
              }`}
            >
              {tab.label}
              <span
                className={`rounded-full px-1.5 py-0.5 text-xs tabular-nums ${
                  step === tab.step ? 'bg-blue-50 text-blue-700' : 'bg-slate-100 text-slate-500'
                }`}
              >
                {formatCount(count)}
              </span>
            </button>
          )
        })}
      </div>

      {/* What happened to them, and the export for exactly this slice. */}
      <div className="mb-4 flex flex-wrap items-center justify-between gap-2">
        <div className="flex gap-1">
          {OUTCOME_FILTERS.map((option) => {
            const stats = breakdown(step)
            const count = option.value === 'abandoned' ? stats?.abandoned : stats?.active
            return (
              <button
                key={option.value}
                type="button"
                onClick={() => changeFilter(() => setOutcome(option.value))}
                className={`rounded-lg px-3 py-1.5 text-sm font-semibold transition ${
                  outcome === option.value
                    ? 'bg-blue-50 text-blue-700'
                    : 'text-slate-600 hover:bg-slate-100'
                }`}
              >
                {option.label}
                <span className="ml-1.5 text-xs tabular-nums opacity-70">
                  {formatCount(count ?? 0)}
                </span>
              </button>
            )
          })}
        </div>

        <button
          type="button"
          onClick={handleExport}
          disabled={exporting}
          title="Exportar esta lista a CSV"
          aria-label="Exportar esta lista a CSV"
          className="rounded-lg border border-slate-300 bg-white p-2 text-slate-600 transition hover:bg-slate-50 disabled:opacity-50"
        >
          {exporting ? <SpinnerIcon /> : <DownloadIcon />}
        </button>
      </div>

      {error ? (
        <Card className="mb-4 border-red-200 bg-red-50 text-sm text-red-700">{error}</Card>
      ) : null}

      {data === null ? (
        <p className="py-12 text-center text-sm text-slate-500">Cargando leads…</p>
      ) : (
        <Card className={stale ? 'opacity-60 transition-opacity' : 'transition-opacity'}>
          <LeadsTable leads={data.items} onRecover={setRecovering} />

          {total > 0 ? (
            <div className="mt-4 flex items-center justify-between border-t border-slate-100 pt-4">
              <span className="text-sm text-slate-500 tabular-nums">
                {formatCount(firstRow)}–{formatCount(lastRow)} de {formatCount(total)}
              </span>
              <div className="flex gap-2">
                <button
                  type="button"
                  disabled={page === 1}
                  onClick={() => setPage((current) => current - 1)}
                  className="rounded-lg border border-slate-300 px-3 py-1.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
                >
                  Anterior
                </button>
                <button
                  type="button"
                  disabled={!hasNext}
                  onClick={() => setPage((current) => current + 1)}
                  className="rounded-lg border border-slate-300 px-3 py-1.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
                >
                  Siguiente
                </button>
              </div>
            </div>
          ) : null}
        </Card>
      )}

      {recovering ? (
        <RecoveryModal
          lead={recovering}
          companyName={companyName ?? ''}
          url={portalUrl(sessionToken)}
          onClose={() => setRecovering(null)}
        />
      ) : null}
    </div>
  )
}

function DownloadIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" className="h-5 w-5" aria-hidden>
      <path
        d="M12 3v12m0 0 4-4m-4 4-4-4M4 17v2a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-2"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  )
}

function SpinnerIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" className="h-5 w-5 animate-spin" aria-hidden>
      <circle cx="12" cy="12" r="9" stroke="currentColor" strokeWidth="2" opacity="0.25" />
      <path d="M21 12a9 9 0 0 0-9-9" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
    </svg>
  )
}
