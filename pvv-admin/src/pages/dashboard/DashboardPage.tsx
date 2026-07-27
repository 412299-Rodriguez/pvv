import { LeadDashboard } from '@/features/lead-analytics'

/** Operator home: how the company's own portal is converting. */
export function DashboardPage() {
  return (
    <div>
      <h1 className="mb-6 text-2xl font-semibold tracking-tight text-stone-900">Dashboard</h1>
      <LeadDashboard />
    </div>
  )
}
