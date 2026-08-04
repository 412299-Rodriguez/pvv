import type { LeadListItem } from '@/entities/lead'

/**
 * A lead worth chasing: it walked away, it left a way to reach it, and nobody has
 * written to it yet.
 *
 * The same three conditions are enforced by the backend. This copy exists so the panel
 * does not offer an action that would be refused — it is a courtesy to the operator,
 * not the rule itself.
 */
export function isRecoverable(lead: LeadListItem): boolean {
  return lead.status === 'abandoned' && Boolean(lead.email) && !lead.recoveredAt
}
