import { useState } from 'react'

import type { LeadListItem } from '@/entities/lead'
import { Button, inputClass } from '@/shared/ui'

import { buildRecoveryDraft } from '../lib/recoveryTemplate'

interface RecoveryModalProps {
  lead: LeadListItem
  companyName: string
  /** Portal link to drop into the email, when the tenant is known. */
  url: string | null
  onClose: () => void
}

/**
 * Composes the "come back and finish" email for an abandoned lead.
 *
 * Sending happens in the operator's own mail client via a mailto: link — the
 * reply lands in their inbox and the message goes out from their real address,
 * which no SMTP setup here would improve on.
 */
export function RecoveryModal({ lead, companyName, url, onClose }: RecoveryModalProps) {
  const draft = buildRecoveryDraft(lead, companyName, url)
  const [subject, setSubject] = useState(draft.subject)
  const [body, setBody] = useState(draft.body)
  const [copied, setCopied] = useState(false)

  const openMailClient = () => {
    const to = encodeURIComponent(lead.email ?? '')
    window.location.href = `mailto:${to}?subject=${encodeURIComponent(
      subject,
    )}&body=${encodeURIComponent(body)}`
  }

  const copyBody = async () => {
    await navigator.clipboard.writeText(body)
    setCopied(true)
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-stone-900/40 p-4"
      role="dialog"
      aria-modal="true"
      aria-label="Recuperar lead"
      onClick={onClose}
    >
      <div
        className="max-h-full w-full max-w-xl overflow-y-auto rounded-xl bg-white p-6 shadow-xl"
        onClick={(event) => event.stopPropagation()}
      >
        <h2 className="text-lg font-bold text-stone-800">Recuperar este lead</h2>
        <p className="mt-1 text-sm text-stone-500">
          Para <span className="font-medium text-stone-700">{lead.email}</span>
          {lead.holderName ? ` · ${lead.holderName}` : ''}
        </p>

        <label className="mt-4 block text-sm font-medium text-stone-600">
          Asunto
          <input
            className={`${inputClass} mt-1`}
            value={subject}
            onChange={(event) => setSubject(event.target.value)}
          />
        </label>

        <label className="mt-4 block text-sm font-medium text-stone-600">
          Mensaje
          <textarea
            className={`${inputClass} mt-1 h-64 font-mono text-xs`}
            value={body}
            onChange={(event) => setBody(event.target.value)}
          />
        </label>

        {url === null ? (
          <p className="mt-2 text-xs text-amber-700">
            No pudimos armar el link al portal, así que el mensaje no lo incluye.
          </p>
        ) : null}

        <div className="mt-5 flex flex-wrap items-center justify-end gap-2">
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg border border-stone-300 px-3 py-2 text-sm font-semibold text-stone-700 transition hover:bg-stone-50"
          >
            Cancelar
          </button>
          <button
            type="button"
            onClick={copyBody}
            className="rounded-lg border border-stone-300 px-3 py-2 text-sm font-semibold text-stone-700 transition hover:bg-stone-50"
          >
            {copied ? 'Copiado' : 'Copiar texto'}
          </button>
          <Button onClick={openMailClient}>Abrir en mi correo</Button>
        </div>
      </div>
    </div>
  )
}
