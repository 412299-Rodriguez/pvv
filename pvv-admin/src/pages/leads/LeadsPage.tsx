import { LeadsExplorer } from '@/features/lead-analytics'

/** Every purchase attempt on the company's portal, with who to call back. */
export function LeadsPage() {
  return (
    <div>
      <h1 className="mb-6 text-2xl font-semibold tracking-tight text-stone-900">Leads</h1>
      <LeadsExplorer />
    </div>
  )
}
