import { useEffect, useState } from 'react'

import {
  FUNNEL_STEPS,
  listLeads,
  type LeadFilters,
  type LeadListItem,
  type LeadStatus,
  type PagedLeads,
} from '@/entities/lead'
import { useSessionStore } from '@/entities/session'
import { RecoveryModal, portalUrl } from '@/features/lead-recovery'
import { Card } from '@/shared/ui'
import { formatCount } from '@/shared/ui/viz'
import { presetToFilter, type RangePreset } from '@/shared/lib'
import { DateRangeFilter } from '@/widgets/date-range-filter'
import { LeadsTable } from '@/widgets/leads-table'

import { exportLeadsCsv } from '../lib/exportLeads'

const PAGE_SIZE = 20
const PORTAL_BASE_URL = import.meta.env.VITE_PORTAL_BASE_URL ?? 'http://localhost:5173'

/** The four states a purchase attempt can be in, as the operator thinks of them. */
const STATUS_TABS: { value: LeadStatus | 'all'; label: string }[] = [
  { value: 'all', label: 'Todos' },
  { value: 'active', label: 'En curso' },
  { value: 'abandoned', label: 'Abandonados' },
  { value: 'completed', label: 'Compraron' },
]

interface LeadsExplorerProps {
  /** SystemAdmin only; an operator is pinned to its own company by its token. */
  companyToken?: string
  /** Signs the recovery email. Omitted for an operator, which doesn't load it. */
  companyName?: string
}

/** The leads table with its filters, tabs, paging, export and recovery action. */
export function LeadsExplorer({ companyToken, companyName }: LeadsExplorerProps) {
  const [preset, setPreset] = useState<RangePreset>('30d')
  const [status, setStatus] = useState<LeadStatus | 'all'>('all')
  const [lastStep, setLastStep] = useState<number | 'all'>('all')
  const [page, setPage] = useState(1)
  const [error, setError] = useState<string | null>(null)
  const [exporting, setExporting] = useState(false)
  const [recovering, setRecovering] = useState<LeadListItem | null>(null)

  // The admin explicitly targets a company; an operator carries its own in the token.
  const sessionToken = useSessionStore((s) => s.companyToken)
  const effectiveToken = companyToken ?? sessionToken

  const filterKey = `${preset}|${status}|${lastStep}|${page}|${companyToken ?? ''}`
  const [loaded, setLoaded] = useState<{ key: string; data: PagedLeads } | null>(null)

  useEffect(() => {
    let active = true

    const load = () => {
      const filters: LeadFilters = {
        ...presetToFilter(preset),
        ...(status === 'all' ? {} : { status }),
        ...(lastStep === 'all' ? {} : { lastStep }),
        ...(companyToken ? { companyToken } : {}),
      }

      listLeads(filters, page, PAGE_SIZE)
        .then((data) => {
          if (!active) return
          setLoaded({ key: filterKey, data })
          setError(null)
        })
        .catch(() => {
          if (active) setError('No pudimos cargar los leads.')
        })
    }

    load()
    return () => {
      active = false
    }
  }, [filterKey, preset, status, lastStep, page, companyToken])

  // Any filter change restarts the paging — page 7 of the old slice is meaningless.
  const changeFilter = (apply: () => void) => {
    apply()
    setPage(1)
  }

  /** Exports what is on screen — the same filters, every page of them. */
  const handleExport = async () => {
    setExporting(true)
    setError(null)
    try {
      const result = await exportLeadsCsv({
        ...presetToFilter(preset),
        ...(status === 'all' ? {} : { status }),
        ...(lastStep === 'all' ? {} : { lastStep }),
        ...(companyToken ? { companyToken } : {}),
      })
      if (result.truncated) {
        setError(`El archivo trae las primeras ${formatCount(result.rows)} filas; hay más.`)
      }
    } catch {
      setError('No pudimos generar el archivo.')
    } finally {
      setExporting(false)
    }
  }

  const data = loaded?.data ?? null
  const stale = loaded !== null && loaded.key !== filterKey

  const total = data?.total ?? 0
  const firstRow = total === 0 ? 0 : (page - 1) * PAGE_SIZE + 1
  const lastRow = Math.min(page * PAGE_SIZE, total)
  const hasNext = page * PAGE_SIZE < total

  return (
    <div>
      <DateRangeFilter
        value={preset}
        onChange={(next) => changeFilter(() => setPreset(next))}
      >
        <select
          value={lastStep}
          onChange={(event) =>
            changeFilter(() =>
              setLastStep(event.target.value === 'all' ? 'all' : Number(event.target.value)),
            )
          }
          className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700"
        >
          <option value="all">Todos los pasos</option>
          <option value="0">Solo entró al portal</option>
          {FUNNEL_STEPS.map((step) => (
            <option key={step.step} value={step.step}>
              {step.step}. {step.label}
            </option>
          ))}
        </select>

        <button
          type="button"
          onClick={handleExport}
          disabled={exporting}
          className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:opacity-50"
        >
          {exporting ? 'Generando…' : 'Exportar'}
        </button>
      </DateRangeFilter>

      <div className="mb-4 flex flex-wrap gap-1">
        {STATUS_TABS.map((tab) => (
          <button
            key={tab.value}
            type="button"
            onClick={() => changeFilter(() => setStatus(tab.value))}
            className={`rounded-lg px-3 py-2 text-sm font-semibold transition ${
              status === tab.value
                ? 'bg-blue-50 text-blue-700'
                : 'text-slate-600 hover:bg-slate-100'
            }`}
          >
            {tab.label}
          </button>
        ))}
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
          url={portalUrl(effectiveToken, PORTAL_BASE_URL)}
          onClose={() => setRecovering(null)}
        />
      ) : null}
    </div>
  )
}
