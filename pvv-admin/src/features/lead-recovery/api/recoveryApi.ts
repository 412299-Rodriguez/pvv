import axios from 'axios'

import { bffInstance } from '@/shared/api'

/**
 * Outcomes the BFF distinguishes. They are not interchangeable: "no address" is a
 * property of the lead, "already sent" is a property of our own history, and a provider
 * failure is temporary. An operator deserves to know which one happened.
 */
export type RecoveryOutcome =
  | 'sent'
  | 'not_found'
  | 'no_contact'
  | 'already_recovered'
  | 'send_failed'

export interface RecoveryResult {
  outcome: RecoveryOutcome
  error?: string
}

/**
 * Asks the BFF to send the recovery email for a lead. The message itself is composed
 * server-side from the company's configured template — this call carries no copy.
 */
export async function sendRecoveryEmail(flowId: string): Promise<RecoveryResult> {
  try {
    await bffInstance.post(`/api/leads/${encodeURIComponent(flowId)}/recover`)
    return { outcome: 'sent' }
  } catch (error) {
    if (axios.isAxiosError(error) && error.response) {
      const data = error.response.data as { status?: string; error?: string } | undefined
      return {
        outcome: (data?.status as RecoveryOutcome) ?? 'send_failed',
        ...(data?.error ? { error: data.error } : {}),
      }
    }
    return { outcome: 'send_failed', error: 'No pudimos contactar al servidor.' }
  }
}
