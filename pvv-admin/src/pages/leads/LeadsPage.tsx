import { useSessionStore } from '@/entities/session'
import { LeadsExplorer } from '@/features/lead-analytics'

/** Every purchase attempt on the company's portal, with who to call back. */
export function LeadsPage() {
  // Signs the recovery email with the company the operator belongs to.
  const companyName = useSessionStore((s) => s.companyName)

  return (
    <div>
      <h1 className="mb-6 text-2xl font-semibold tracking-tight text-stone-900">Leads</h1>
      <LeadsExplorer {...(companyName ? { companyName } : {})} />
    </div>
  )
}
