import { LeadDashboard } from '@/features/lead-analytics'

/** Operator home: how the company's own portal is converting. */
export function DashboardPage() {
  return (
    <div>
      <h1 className="mb-6 text-xl font-bold text-slate-800">Dashboard</h1>
      <LeadDashboard />
    </div>
  )
}
