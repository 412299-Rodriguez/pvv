/**
 * Leads as pvv-bff serves them. A lead is one purchase attempt in the portal,
 * built up step by step from the events the wizard reports.
 */

/** How far the buyer got. The five funnel milestones, in order. */
export const FUNNEL_STEPS = [
  { step: 1, label: 'Vehículo' },
  { step: 2, label: 'Tomador' },
  { step: 3, label: 'Cotización' },
  { step: 4, label: 'Pago' },
  { step: 5, label: 'Emisión' },
] as const

export type LeadStatus = 'active' | 'abandoned' | 'completed'

export interface LeadFunnelStep {
  step: number
  label: string
  /** Leads that got at least this far. */
  reached: number
  /** Share of the previous rung that made it here, 0-100. */
  conversionFromPrevious: number
}

export interface LeadFunnel {
  totalLeads: number
  active: number
  abandoned: number
  completed: number
  policiesIssued: number
  overallConversion: number
  steps: LeadFunnelStep[]
}

export interface LeadListItem {
  flowId: string
  createdAt: string
  updatedAt: string
  lastStep: number
  status: string
  plate: string | null
  vehicleTitle: string | null
  holderName: string | null
  dni: string | null
  email: string | null
  phone: string | null
  productName: string | null
  amount: number | null
  /** Status of the payment step: initiated | completed | rejected | abandoned. */
  paymentStatus: string | null
  policyNumber: string | null
}

export interface PagedLeads {
  items: LeadListItem[]
  total: number
  page: number
  pageSize: number
}

/** Query filters shared by the funnel and the table. */
export interface LeadFilters {
  /** ISO date; omitted means "since the beginning". */
  from?: string
  to?: string
  lastStep?: number
  status?: LeadStatus
  /** SystemAdmin only — an operator is pinned to its own company by its token. */
  companyToken?: string
}
