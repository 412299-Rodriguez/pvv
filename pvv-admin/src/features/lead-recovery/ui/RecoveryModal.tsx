import { useState } from 'react'

import type { LeadListItem } from '@/entities/lead'
import { Button } from '@/shared/ui'

import { sendRecoveryEmail, type RecoveryOutcome } from '../api/recoveryApi'

interface RecoveryModalProps {
  lead: LeadListItem
  onClose: () => void
  /** Called after a successful send so the table can pick up the new state. */
  onSent: () => void
}

/** What each outcome means to the person who pressed the button. */
const OUTCOME_MESSAGE: Record<Exclude<RecoveryOutcome, 'sent'>, string> = {
  not_found: 'No encontramos este lead. Puede que se haya borrado.',
  no_contact: 'Este lead no dejó una dirección de correo, así que no se le puede escribir.',
  already_recovered: 'A este lead ya se le envió el correo de recupero.',
  send_failed: 'No pudimos enviar el correo. Probá de nuevo en un rato.',
}

/**
 * Confirms sending the recovery email for an abandoned lead.
 *
 * It confirms rather than composes. The wording lives in the company's Recupero
 * settings and the message is assembled and sent by the backend, which is what makes it
 * consistent, branded and recorded. The previous version opened the operator's own mail
 * client with an editable draft: whatever reached the customer depended on the machine
 * it was sent from, and nothing was ever written down.
 */
export function RecoveryModal({ lead, onClose, onSent }: RecoveryModalProps) {
  const [sending, setSending] = useState(false)
  const [outcome, setOutcome] = useState<RecoveryOutcome | null>(null)
  const [error, setError] = useState<string | null>(null)

  const send = async () => {
    setSending(true)
    setError(null)
    const result = await sendRecoveryEmail(lead.flowId)
    setSending(false)
    setOutcome(result.outcome)

    if (result.outcome === 'sent') {
      onSent()
      return
    }
    setError(result.error ?? OUTCOME_MESSAGE[result.outcome])
  }

  const sent = outcome === 'sent'

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-stone-900/40 p-4"
      role="dialog"
      aria-modal="true"
      aria-label="Recuperar lead"
      onClick={onClose}
    >
      <div
        className="w-full max-w-md rounded-xl bg-white p-6 shadow-xl"
        onClick={(event) => event.stopPropagation()}
      >
        {sent ? (
          <>
            <h2 className="text-lg font-bold text-stone-800">Correo enviado</h2>
            <p className="mt-2 text-sm text-stone-600">
              Le escribimos a <span className="font-medium text-stone-800">{lead.email}</span>{' '}
              invitándolo a retomar su compra. Queda registrado, así que no se le va a
              volver a escribir.
            </p>
            <div className="mt-5 flex justify-end">
              <Button onClick={onClose}>Listo</Button>
            </div>
          </>
        ) : (
          <>
            <h2 className="text-lg font-bold text-stone-800">Recuperar este lead</h2>
            <p className="mt-2 text-sm text-stone-600">
              Se le va a enviar un correo a{' '}
              <span className="font-medium text-stone-800">{lead.email}</span>
              {lead.holderName ? ` (${lead.holderName})` : ''} invitándolo a retomar la compra
              {lead.plate ? ` de ${lead.plate}` : ''}.
            </p>
            <p className="mt-3 text-xs text-stone-500">
              El texto sale de lo que configuraste en <span className="font-medium">Recupero</span>,
              con el logo y los colores de tu compañía. Se envía una sola vez por lead.
            </p>

            {error ? (
              <p className="mt-4 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
                {error}
              </p>
            ) : null}

            <div className="mt-5 flex items-center justify-end gap-2">
              <button
                type="button"
                onClick={onClose}
                className="rounded-lg border border-stone-300 px-3 py-2 text-sm font-semibold text-stone-700 transition hover:bg-stone-50"
              >
                Cancelar
              </button>
              <Button onClick={send} disabled={sending}>
                {sending ? 'Enviando…' : 'Enviar correo'}
              </Button>
            </div>
          </>
        )}
      </div>
    </div>
  )
}
