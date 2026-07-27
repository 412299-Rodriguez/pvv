import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'

import { getCompany, type Company } from '@/entities/company'
import { AppearanceEditor } from '@/features/edit-appearance'
import { ProductsEditor } from '@/features/edit-products'
import { LeadDashboard, LeadsExplorer } from '@/features/lead-analytics'

type Tab = 'dashboard' | 'leads' | 'apariencia' | 'productos'

const TABS: { value: Tab; label: string }[] = [
  { value: 'dashboard', label: 'Dashboard' },
  { value: 'leads', label: 'Leads' },
  { value: 'apariencia', label: 'Apariencia' },
  { value: 'productos', label: 'Productos' },
]

export function CompanyDetailPage() {
  const { id = '' } = useParams()
  const [company, setCompany] = useState<Company | null>(null)
  const [tab, setTab] = useState<Tab>('dashboard')

  useEffect(() => {
    let active = true
    getCompany(id)
      .then((data) => active && setCompany(data))
      .catch(() => {})
    return () => {
      active = false
    }
  }, [id])

  return (
    <div className="space-y-5">
      <div>
        <Link to="/companias" className="text-sm font-semibold text-blue-600 hover:underline">
          ← Compañías
        </Link>
        <h1 className="mt-1 text-2xl font-bold text-slate-800">{company?.name ?? 'Compañía'}</h1>
        {company && <p className="text-sm text-slate-400">CUIT {company.cuit}</p>}
      </div>

      <div className="flex gap-1 border-b border-slate-200">
        {TABS.map((option) => (
          <button
            key={option.value}
            type="button"
            onClick={() => setTab(option.value)}
            className={`-mb-px border-b-2 px-4 py-2 text-sm font-semibold transition ${
              tab === option.value
                ? 'border-blue-600 text-blue-700'
                : 'border-transparent text-slate-500 hover:text-slate-700'
            }`}
          >
            {option.label}
          </button>
        ))}
      </div>

      {/* Analytics needs the portal hash, which only arrives with the company. */}
      {tab === 'dashboard' || tab === 'leads' ? (
        company === null ? (
          <p className="py-12 text-center text-sm text-slate-500">Cargando compañía…</p>
        ) : tab === 'dashboard' ? (
          <LeadDashboard key={id} companyToken={company.hashedCompanyId} />
        ) : (
          <LeadsExplorer key={id} companyToken={company.hashedCompanyId} />
        )
      ) : tab === 'apariencia' ? (
        <AppearanceEditor key={id} companyId={id} />
      ) : (
        <ProductsEditor key={id} companyId={id} />
      )}
    </div>
  )
}
