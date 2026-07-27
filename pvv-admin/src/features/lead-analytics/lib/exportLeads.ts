import { FUNNEL_STEPS, listLeads, type LeadFilters, type LeadListItem } from '@/entities/lead'

/** Ceiling on an export, so a huge slice cannot hang the browser. */
const MAX_ROWS = 10_000
/** Rows per request while walking the pages. */
const FETCH_PAGE_SIZE = 200

const COLUMNS = [
  'Fecha',
  'Llego hasta',
  'Estado',
  'Patente',
  'Vehiculo',
  'Nombre',
  'DNI',
  'Email',
  'Telefono',
  'Producto',
  'Monto',
  'Estado del pago',
  'Poliza',
] as const

const STATUS_LABELS: Record<string, string> = {
  active: 'En curso',
  abandoned: 'Abandonó',
  completed: 'Compró',
}

/**
 * Downloads the current slice as a spreadsheet.
 *
 * Semicolon-separated with a UTF-8 BOM: that is what Excel in a Spanish locale
 * expects, and without the BOM every accent arrives mangled. Returns how many
 * rows were written, and whether the ceiling cut the export short.
 */
export async function exportLeadsCsv(
  filters: LeadFilters,
): Promise<{ rows: number; truncated: boolean }> {
  // The first page also tells us how many there are in total.
  const first = await listLeads(filters, 1, FETCH_PAGE_SIZE)
  const total = first.total
  const leads: LeadListItem[] = [...first.items]

  // Walk the rest until we have them all (or hit the ceiling).
  let page = 2
  while (leads.length < total && leads.length < MAX_ROWS) {
    const result = await listLeads(filters, page, FETCH_PAGE_SIZE)
    if (result.items.length === 0) break

    leads.push(...result.items)
    page += 1
  }

  const rows = leads.slice(0, MAX_ROWS)
  download(toCsv(rows), `leads-${new Date().toISOString().slice(0, 10)}.csv`)

  return { rows: rows.length, truncated: total > rows.length }
}

function toCsv(leads: LeadListItem[]): string {
  const lines = [COLUMNS.join(';')]

  for (const lead of leads) {
    const step = FUNNEL_STEPS.find((s) => s.step === lead.lastStep)
    lines.push(
      [
        new Date(lead.createdAt).toLocaleString('es-AR'),
        lead.lastStep < 1 ? 'Solo entró al portal' : `${lead.lastStep}. ${step?.label ?? ''}`,
        STATUS_LABELS[lead.status] ?? lead.status,
        lead.plate,
        lead.vehicleTitle,
        lead.holderName,
        lead.dni,
        lead.email,
        lead.phone,
        lead.productName,
        lead.amount,
        lead.paymentStatus,
        lead.policyNumber,
      ]
        .map(escapeCell)
        .join(';'),
    )
  }

  return lines.join('\r\n')
}

/** Quotes a cell only when it needs it, doubling any quote inside. */
function escapeCell(value: string | number | null): string {
  if (value === null || value === undefined) return ''

  const text = String(value)
  return /[";\r\n]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text
}

function download(csv: string, filename: string): void {
  // The BOM (escaped, not literal) is what tells Excel the file is UTF-8.
  const blob = new Blob(['\ufeff' + csv], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)

  const link = document.createElement('a')
  link.href = url
  link.download = filename
  link.click()

  URL.revokeObjectURL(url)
}
