import { useEffect, useState } from 'react'

import {
  CONFIG_TYPE,
  RECOVERY_DEFAULT_HINTS,
  RECOVERY_PLACEHOLDERS,
  emptyRecoveryEmailConfig,
  getConfig,
  putConfig,
  type RecoveryEmailConfig,
} from '@/entities/config'
import { Button, Card, inputClass } from '@/shared/ui'

type SaveStatus = 'idle' | 'saving' | 'saved' | 'error'

/**
 * Edits a company's RECOVERY_EMAIL_CONFIG — the copy of the email sent to somebody who
 * abandoned a purchase.
 *
 * Only the words are editable. The layout, the company's logo and colour, and the button
 * that leads back to the portal are assembled by pvv-bff when it sends: an operator
 * editing a sentence should not have to keep an HTML email working across mail clients,
 * and one unclosed tag here would reach a customer's inbox.
 */
export function RecoveryEmailEditor({ companyId }: { companyId: string }) {
  const [config, setConfig] = useState<RecoveryEmailConfig>(emptyRecoveryEmailConfig)
  const [loading, setLoading] = useState(true)
  const [status, setStatus] = useState<SaveStatus>('idle')

  useEffect(() => {
    let active = true
    getConfig<RecoveryEmailConfig>(companyId, CONFIG_TYPE.recoveryEmail, emptyRecoveryEmailConfig)
      .then((data) => {
        if (active) {
          setConfig({ ...emptyRecoveryEmailConfig, ...data })
          setLoading(false)
        }
      })
      .catch(() => active && setLoading(false))
    return () => {
      active = false
    }
  }, [companyId])

  const patch = (changes: Partial<RecoveryEmailConfig>) => setConfig((c) => ({ ...c, ...changes }))

  const save = async () => {
    setStatus('saving')
    try {
      await putConfig(companyId, CONFIG_TYPE.recoveryEmail, config)
      setStatus('saved')
      window.setTimeout(() => setStatus('idle'), 2500)
    } catch {
      setStatus('error')
    }
  }

  if (loading) {
    return <p className="text-sm text-stone-500">Cargando configuración…</p>
  }

  return (
    <div>
      <div className="mb-5 flex items-center justify-between">
        <h1 className="text-2xl font-semibold tracking-tight text-stone-900">Correo de recupero</h1>
        <div className="flex items-center gap-3">
          {status === 'saved' && <span className="text-sm font-medium text-green-600">Guardado ✓</span>}
          {status === 'error' && <span className="text-sm font-medium text-red-600">Error al guardar</span>}
          <Button onClick={save} disabled={status === 'saving'}>
            {status === 'saving' ? 'Guardando…' : 'Guardar cambios'}
          </Button>
        </div>
      </div>

      <p className="mb-5 max-w-2xl text-sm text-stone-500">
        Es el mensaje que se le envía a quien empezó una cotización y no la terminó. Se manda
        una sola vez por persona, y solo a quienes dejaron su correo. El logo, el color y el
        botón se arman solos con la identidad de tu compañía.
      </p>

      <Card>
        <div className="grid gap-5">
          <Row
            label="Asunto"
            value={config.Subject}
            hint={RECOVERY_DEFAULT_HINTS.Subject}
            onChange={(v) => patch({ Subject: v })}
          />
          <Row
            label="Saludo"
            value={config.Greeting}
            hint={RECOVERY_DEFAULT_HINTS.Greeting}
            onChange={(v) => patch({ Greeting: v })}
          />
          <Row
            label="Mensaje"
            value={config.Intro}
            hint={RECOVERY_DEFAULT_HINTS.Intro}
            onChange={(v) => patch({ Intro: v })}
            multiline
          />
          <Row
            label="Texto del botón"
            value={config.ButtonLabel}
            hint={RECOVERY_DEFAULT_HINTS.ButtonLabel}
            onChange={(v) => patch({ ButtonLabel: v })}
          />
          <Row
            label="Cierre"
            value={config.Closing}
            hint={RECOVERY_DEFAULT_HINTS.Closing}
            onChange={(v) => patch({ Closing: v })}
          />
        </div>

        <div className="mt-6 border-t border-stone-200 pt-4">
          <p className="text-sm font-medium text-stone-700">Datos que podés insertar</p>
          <p className="mt-1 text-xs text-stone-500">
            Escribilos tal cual en cualquiera de los campos. Se reemplazan por los datos del
            lead al enviar; si un dato falta, queda vacío.
          </p>
          <div className="mt-3 flex flex-wrap gap-2">
            {RECOVERY_PLACEHOLDERS.map((p) => (
              <span
                key={p.token}
                title={p.label}
                className="rounded border border-stone-200 bg-stone-50 px-2 py-1 font-mono text-xs text-stone-700"
              >
                {p.token}
              </span>
            ))}
          </div>
        </div>
      </Card>
    </div>
  )
}

/**
 * One field. The default text is the placeholder rather than a prefilled value, so an
 * empty box is an honest statement: nothing was written here, and the system's own
 * wording is what goes out.
 */
function Row({
  label,
  value,
  hint,
  onChange,
  multiline = false,
}: {
  label: string
  value: string
  hint: string
  onChange: (value: string) => void
  multiline?: boolean
}) {
  return (
    <label className="block text-sm font-medium text-stone-600">
      {label}
      {multiline ? (
        <textarea
          className={`${inputClass} mt-1 h-28`}
          value={value}
          placeholder={hint}
          onChange={(event) => onChange(event.target.value)}
        />
      ) : (
        <input
          className={`${inputClass} mt-1`}
          value={value}
          placeholder={hint}
          onChange={(event) => onChange(event.target.value)}
        />
      )}
      {value.trim() === '' && (
        <span className="mt-1 block text-xs font-normal text-stone-400">
          Vacío: se usa el texto por defecto que ves en gris.
        </span>
      )}
    </label>
  )
}
