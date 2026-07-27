import { bffInstance } from '@/shared/api'

import type { LeadFilters, LeadFunnel, PagedLeads } from '../model/types'

/** Turns the filter object into query params, dropping the empty ones. */
function toParams(filters: LeadFilters): Record<string, string | number> {
  const params: Record<string, string | number> = {}
  if (filters.from) params.from = filters.from
  if (filters.to) params.to = filters.to
  if (filters.lastStep !== undefined) params.lastStep = filters.lastStep
  if (filters.status) params.status = filters.status
  return params
}

/** Conversion funnel and headline numbers for the dashboard. */
export async function getLeadFunnel(filters: LeadFilters = {}): Promise<LeadFunnel> {
  const { data } = await bffInstance.get<LeadFunnel>('/api/leads/funnel', {
    params: toParams(filters),
  })
  return data
}

/** One page of the leads table, newest first. */
export async function listLeads(
  filters: LeadFilters = {},
  page = 1,
  pageSize = 20,
): Promise<PagedLeads> {
  const { data } = await bffInstance.get<PagedLeads>('/api/leads', {
    params: { ...toParams(filters), page, pageSize },
  })
  return data
}
