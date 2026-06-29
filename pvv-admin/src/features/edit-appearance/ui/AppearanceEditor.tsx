import { useEffect, useState } from 'react'

import {
  CONFIG_TYPE,
  UI_TEXT_FIELDS,
  emptyUiConfig,
  getConfig,
  putConfig,
  type UiConfig,
} from '@/entities/config'
import { Button, Card, Field, inputClass } from '@/shared/ui'

type SubTab = 'branding' | 'textos' | 'anuncios' | 'legales' | 'faq'
type SaveStatus = 'idle' | 'saving' | 'saved' | 'error'

const SUBTABS: { key: SubTab; label: string }[] = [
  { key: 'branding', label: 'Branding' },
  { key: 'textos', label: 'Textos' },
  { key: 'anuncios', label: 'Anuncios' },
  { key: 'legales', label: 'Legales' },
  { key: 'faq', label: 'FAQ' },
]

/** Edits a company's PVV_UI_CONFIG (appearance). Reusable: pass the companyId. */
export function AppearanceEditor({ companyId }: { companyId: string }) {
  const [config, setConfig] = useState<UiConfig>(emptyUiConfig)
  const [loading, setLoading] = useState(true)
  const [tab, setTab] = useState<SubTab>('branding')
  const [status, setStatus] = useState<SaveStatus>('idle')

  useEffect(() => {
    let active = true
    setLoading(true)
    getConfig<UiConfig>(companyId, CONFIG_TYPE.ui, emptyUiConfig)
      .then((data) => {
        if (active) {
          setConfig({ ...emptyUiConfig, ...data })
          setLoading(false)
        }
      })
      .catch(() => active && setLoading(false))
    return () => {
      active = false
    }
  }, [companyId])

  const patch = (changes: Partial<UiConfig>) => setConfig((c) => ({ ...c, ...changes }))
  const setText = (key: string, value: string) =>
    setConfig((c) => ({ ...c, Texts: { ...c.Texts, [key]: value } }))

  const save = async () => {
    setStatus('saving')
    try {
      await putConfig(companyId, CONFIG_TYPE.ui, config)
      setStatus('saved')
      window.setTimeout(() => setStatus('idle'), 2500)
    } catch {
      setStatus('error')
    }
  }

  if (loading) {
    return <p className="text-sm text-slate-500">Cargando configuración…</p>
  }

  return (
    <div>
      <div className="mb-5 flex items-center justify-between">
        <h1 className="text-2xl font-bold text-slate-800">Apariencia</h1>
        <div className="flex items-center gap-3">
          {status === 'saved' && <span className="text-sm font-medium text-green-600">Guardado ✓</span>}
          {status === 'error' && <span className="text-sm font-medium text-red-600">Error al guardar</span>}
          <Button onClick={save} disabled={status === 'saving'}>
            {status === 'saving' ? 'Guardando…' : 'Guardar cambios'}
          </Button>
        </div>
      </div>

      {/* Subtabs */}
      <div className="mb-5 flex gap-1 border-b border-slate-200">
        {SUBTABS.map((s) => (
          <button
            key={s.key}
            type="button"
            onClick={() => setTab(s.key)}
            className={`-mb-px border-b-2 px-4 py-2 text-sm font-semibold transition ${
              tab === s.key
                ? 'border-blue-600 text-blue-700'
                : 'border-transparent text-slate-500 hover:text-slate-700'
            }`}
          >
            {s.label}
          </button>
        ))}
      </div>

      <Card>
        {tab === 'branding' && (
          <div className="grid gap-5 sm:grid-cols-2">
            <Field label="Color primario">
              <div className="flex items-center gap-2">
                <input
                  type="color"
                  value={config.PrimaryColor}
                  onChange={(e) => patch({ PrimaryColor: e.target.value })}
                  className="h-10 w-12 rounded border border-slate-300"
                />
                <input
                  value={config.PrimaryColor}
                  onChange={(e) => patch({ PrimaryColor: e.target.value })}
                  className={inputClass}
                />
              </div>
            </Field>
            <Field label="Color secundario">
              <div className="flex items-center gap-2">
                <input
                  type="color"
                  value={config.SecondaryColor}
                  onChange={(e) => patch({ SecondaryColor: e.target.value })}
                  className="h-10 w-12 rounded border border-slate-300"
                />
                <input
                  value={config.SecondaryColor}
                  onChange={(e) => patch({ SecondaryColor: e.target.value })}
                  className={inputClass}
                />
              </div>
            </Field>
            <Field label="Nombre de la compañía">
              <input
                value={config.CompanyDisplayName}
                onChange={(e) => patch({ CompanyDisplayName: e.target.value })}
                className={inputClass}
              />
            </Field>
            <Field label="Logo (URL)" hint="URL pública de la imagen del logo.">
              <input
                value={config.LogoUrl}
                onChange={(e) => patch({ LogoUrl: e.target.value })}
                className={inputClass}
              />
            </Field>
            <Field label="Texto de bienvenida">
              <input
                value={config.WelcomeText}
                onChange={(e) => patch({ WelcomeText: e.target.value })}
                className={inputClass}
              />
            </Field>
            <Field label="Texto del pie">
              <input
                value={config.FooterText}
                onChange={(e) => patch({ FooterText: e.target.value })}
                className={inputClass}
              />
            </Field>
          </div>
        )}

        {tab === 'textos' && (
          <div className="grid gap-4 sm:grid-cols-2">
            {UI_TEXT_FIELDS.map((f) => (
              <Field key={f.key} label={f.label}>
                <input
                  value={config.Texts[f.key] ?? ''}
                  onChange={(e) => setText(f.key, e.target.value)}
                  className={inputClass}
                />
              </Field>
            ))}
          </div>
        )}

        {tab === 'anuncios' && (
          <div className="space-y-3">
            <p className="text-sm text-slate-500">Imágenes (URL pública) del carrusel de publicidad.</p>
            {config.AdImages.map((url, i) => (
              <div key={i} className="flex items-center gap-2">
                <input
                  value={url}
                  onChange={(e) =>
                    patch({ AdImages: config.AdImages.map((u, idx) => (idx === i ? e.target.value : u)) })
                  }
                  className={inputClass}
                  placeholder="https://…/banner.jpg"
                />
                <Button
                  variant="danger"
                  onClick={() => patch({ AdImages: config.AdImages.filter((_, idx) => idx !== i) })}
                >
                  Quitar
                </Button>
              </div>
            ))}
            <Button variant="secondary" onClick={() => patch({ AdImages: [...config.AdImages, ''] })}>
              + Agregar imagen
            </Button>
          </div>
        )}

        {tab === 'legales' && (
          <div className="space-y-5">
            <Field label="Términos y Condiciones">
              <textarea
                value={config.TermsText}
                onChange={(e) => patch({ TermsText: e.target.value })}
                rows={8}
                className={inputClass}
              />
            </Field>
            <Field label="Política de Privacidad">
              <textarea
                value={config.PrivacyText}
                onChange={(e) => patch({ PrivacyText: e.target.value })}
                rows={8}
                className={inputClass}
              />
            </Field>
          </div>
        )}

        {tab === 'faq' && (
          <div className="space-y-4">
            {config.Faqs.map((faq, i) => (
              <div key={i} className="space-y-2 rounded-lg border border-slate-200 p-4">
                <Field label={`Pregunta ${i + 1}`}>
                  <input
                    value={faq.Question}
                    onChange={(e) =>
                      patch({
                        Faqs: config.Faqs.map((f, idx) =>
                          idx === i ? { ...f, Question: e.target.value } : f,
                        ),
                      })
                    }
                    className={inputClass}
                  />
                </Field>
                <Field label="Respuesta">
                  <textarea
                    value={faq.Answer}
                    onChange={(e) =>
                      patch({
                        Faqs: config.Faqs.map((f, idx) =>
                          idx === i ? { ...f, Answer: e.target.value } : f,
                        ),
                      })
                    }
                    rows={3}
                    className={inputClass}
                  />
                </Field>
                <Button
                  variant="danger"
                  onClick={() => patch({ Faqs: config.Faqs.filter((_, idx) => idx !== i) })}
                >
                  Quitar pregunta
                </Button>
              </div>
            ))}
            <Button
              variant="secondary"
              onClick={() => patch({ Faqs: [...config.Faqs, { Question: '', Answer: '' }] })}
            >
              + Agregar pregunta
            </Button>
          </div>
        )}
      </Card>
    </div>
  )
}
