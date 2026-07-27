import { LeadsExplorer } from '@/features/lead-analytics'

/** Every purchase attempt on the company's portal, with who to call back. */
export function LeadsPage() {
  return (
    <div>
      <h1 className="mb-6 text-xl font-bold text-slate-800">Leads</h1>
      <LeadsExplorer />
    </div>
  )
}
