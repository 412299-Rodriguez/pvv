import { useParams } from 'react-router-dom'

import { PagePlaceholder } from '@/shared/ui'

export function CompanyDetailPage() {
  const { id } = useParams()
  return (
    <PagePlaceholder
      title="Configurar compañía"
      description={`Apariencia y productos de la compañía ${id ?? '—'} (Fase B/C).`}
    />
  )
}
