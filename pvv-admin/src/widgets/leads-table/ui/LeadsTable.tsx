import { FUNNEL_STEPS, type LeadListItem } from '@/entities/lead'
import { isRecoverable } from '@/features/lead-recovery'
import { formatDateTime, orDash } from '@/shared/lib'
import { FUNNEL_RAMP, formatMoney } from '@/shared/ui/viz'

interface LeadsTableProps {
  leads: LeadListItem[]
  /** Offers the recovery action on leads that walked away and left an email. */
  onRecover?: (lead: LeadListItem) => void
}

/**
 * Where each lead stopped and who it belongs to. Reading order matches the
 * question an operator actually asks: when, how far, which car, who to call.
 */
export function LeadsTable({ leads, onRecover }: LeadsTableProps) {
  if (leads.length === 0) {
    return (
      <div className="px-5 py-16 text-center">
        <p className="text-sm font-medium text-slate-600">No hay leads acá</p>
        <p className="mt-1 text-sm text-slate-400">
          Probá con otro paso o cambiá el período.
        </p>
      </div>
    )
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[56rem] text-sm">
        <thead>
          <tr className="border-y border-slate-200 bg-slate-50/60 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
            <th className="py-2.5 pl-5 pr-3">Fecha</th>
            <th className="py-2.5 pr-3">Llegó hasta</th>
            <th className="py-2.5 pr-3">Vehículo</th>
            <th className="py-2.5 pr-3">Contacto</th>
            <th className="py-2.5 pr-3">Producto</th>
            <th className="py-2.5 pr-3">Estado</th>
            <th className="py-2.5 pr-5" />
          </tr>
        </thead>
        <tbody>
          {leads.map((lead) => (
            <tr
              key={lead.flowId}
              className="border-b border-slate-100 align-top transition-colors last:border-0 hover:bg-slate-50"
            >
              <td className="py-3.5 pl-5 pr-3 whitespace-nowrap text-slate-500 tabular-nums">
                {formatDateTime(lead.createdAt)}
              </td>

              <td className="py-3.5 pr-3">
                <StepBadge lastStep={lead.lastStep} />
              </td>

              <td className="py-3.5 pr-3">
                <div className="font-semibold text-slate-800">{orDash(lead.plate)}</div>
                {lead.vehicleTitle ? (
                  <div className="mt-0.5 text-xs text-slate-500">{lead.vehicleTitle}</div>
                ) : null}
              </td>

              <td className="py-3.5 pr-3">
                <div className="font-medium text-slate-800">{orDash(lead.holderName)}</div>
                {lead.email ? (
                  <div className="mt-0.5 text-xs text-slate-500">{lead.email}</div>
                ) : null}
                {lead.phone ? <div className="text-xs text-slate-400">{lead.phone}</div> : null}
              </td>

              <td className="py-3.5 pr-3">
                <div className="text-slate-700">{orDash(lead.productName)}</div>
                {lead.amount !== null ? (
                  <div className="mt-0.5 text-xs font-semibold text-slate-600 tabular-nums">
                    {formatMoney(lead.amount)}
                  </div>
                ) : null}
              </td>

              <td className="py-3.5 pr-3">
                <StatusBadge status={lead.status} paymentStatus={lead.paymentStatus} />
                {lead.policyNumber ? (
                  <div className="mt-1 text-xs text-slate-500 tabular-nums">
                    {lead.policyNumber}
                  </div>
                ) : null}
              </td>

              <td className="py-3.5 pr-5 text-right">
                {onRecover && isRecoverable(lead) ? (
                  <button
                    type="button"
                    onClick={() => onRecover(lead)}
                    className="whitespace-nowrap rounded-lg border border-slate-300 bg-white px-2.5 py-1 text-xs font-semibold text-slate-700 transition hover:border-slate-400 hover:bg-slate-50"
                  >
                    Recuperar
                  </button>
                ) : null}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

/**
 * The furthest milestone reached. The dot repeats the funnel's ordinal ramp so
 * the table and the chart read as the same scale; the text carries the meaning,
 * never the color alone.
 */
function StepBadge({ lastStep }: { lastStep: number }) {
  if (lastStep < 1) {
    return <span className="text-xs text-slate-500">Solo entró al portal</span>
  }

  const step = FUNNEL_STEPS.find((s) => s.step === lastStep)

  return (
    <span className="flex items-center gap-2 whitespace-nowrap text-slate-700">
      <span
        className="inline-block h-2 w-2 shrink-0 rounded-full"
        style={{ backgroundColor: FUNNEL_RAMP[lastStep - 1] ?? FUNNEL_RAMP[0] }}
        aria-hidden
      />
      {lastStep}. {step?.label ?? '—'}
    </span>
  )
}

const STATUS_LABELS: Record<string, { label: string; className: string }> = {
  completed: { label: 'Compró', className: 'bg-emerald-50 text-emerald-700' },
  abandoned: { label: 'Abandonó', className: 'bg-amber-50 text-amber-700' },
  active: { label: 'En curso', className: 'bg-slate-100 text-slate-600' },
}

/** Never color alone: the badge always spells the state out. */
function StatusBadge({ status, paymentStatus }: { status: string; paymentStatus: string | null }) {
  const style = STATUS_LABELS[status] ?? {
    label: status,
    className: 'bg-slate-100 text-slate-600',
  }

  return (
    <div>
      <span
        className={`inline-block rounded-full px-2 py-0.5 text-xs font-semibold ${style.className}`}
      >
        {style.label}
      </span>
      {paymentStatus === 'rejected' ? (
        <div className="mt-1 text-xs text-red-600">Pago rechazado</div>
      ) : null}
    </div>
  )
}
