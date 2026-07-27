import type { LeadListItem } from '@/entities/lead'

/** A lead worth chasing: it walked away, and it left a way to reach it. */
export function isRecoverable(lead: LeadListItem): boolean {
  return lead.status === 'abandoned' && Boolean(lead.email)
}

export interface RecoveryDraft {
  subject: string
  body: string
}

/**
 * Pre-fills the recovery email from what the lead already told us. Everything
 * here is a starting point — the operator edits it before sending.
 */
export function buildRecoveryDraft(
  lead: LeadListItem,
  companyName: string,
  url: string | null,
): RecoveryDraft {
  const firstName = lead.holderName?.split(' ')[0] ?? ''
  const vehicle = lead.vehicleTitle ?? (lead.plate ? `vehículo ${lead.plate}` : 'vehículo')

  const subject = lead.plate
    ? `Tu cotización de SOAT para ${lead.plate} sigue disponible`
    : 'Tu cotización de SOAT sigue disponible'

  const lines = [
    `Hola${firstName ? ` ${firstName}` : ''},`,
    '',
    `Vimos que empezaste a cotizar el seguro de tu ${vehicle} y no llegaste a completar la compra.`,
  ]

  if (lead.productName && lead.amount !== null) {
    lines.push(
      '',
      `Tu cotización de ${lead.productName} por ${lead.amount.toLocaleString('es-AR', {
        style: 'currency',
        currency: 'ARS',
        maximumFractionDigits: 0,
      })} sigue vigente.`,
    )
  }

  if (url) {
    lines.push('', `Podés retomarla desde acá: ${url}`)
  }

  lines.push('', 'Cualquier duda, respondé este mail y te ayudamos.')
  if (companyName) lines.push('', companyName)

  return { subject, body: lines.join('\n') }
}
