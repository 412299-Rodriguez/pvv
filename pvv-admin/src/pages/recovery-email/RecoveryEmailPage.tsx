import { useSessionStore } from '@/entities/session'
import { RecoveryEmailEditor } from '@/features/edit-recovery-email'

export function RecoveryEmailPage() {
  const companyId = useSessionStore((s) => s.companyId)

  if (!companyId) {
    return <p className="text-sm text-stone-500">Tu usuario no tiene una compañía asociada.</p>
  }

  return <RecoveryEmailEditor companyId={companyId} />
}
