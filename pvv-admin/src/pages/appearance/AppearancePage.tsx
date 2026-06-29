import { useSessionStore } from '@/entities/session'
import { PagePlaceholder } from '@/shared/ui'

export function AppearancePage() {
  const companyId = useSessionStore((s) => s.companyId)
  return (
    <PagePlaceholder
      title="Apariencia"
      description={`Branding, textos, anuncios, legales y FAQ de tu compañía (Fase B). Company: ${companyId ?? '—'}.`}
    />
  )
}
