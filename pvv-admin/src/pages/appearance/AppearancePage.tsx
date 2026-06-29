import { useSessionStore } from '@/entities/session'
import { AppearanceEditor } from '@/features/edit-appearance'

export function AppearancePage() {
  const companyId = useSessionStore((s) => s.companyId)

  if (!companyId) {
    return <p className="text-sm text-slate-500">Tu usuario no tiene una compañía asociada.</p>
  }

  return <AppearanceEditor companyId={companyId} />
}
