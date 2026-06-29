import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'

import { getCompany, type Company } from '@/entities/company'
import { AppearanceEditor } from '@/features/edit-appearance'
import { ProductsEditor } from '@/features/edit-products'

type Tab = 'apariencia' | 'productos'

export function CompanyDetailPage() {
  const { id = '' } = useParams()
  const [company, setCompany] = useState<Company | null>(null)
  const [tab, setTab] = useState<Tab>('apariencia')

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
        {(['apariencia', 'productos'] as Tab[]).map((t) => (
          <button
            key={t}
            type="button"
            onClick={() => setTab(t)}
            className={`-mb-px border-b-2 px-4 py-2 text-sm font-semibold capitalize transition ${
              tab === t ? 'border-blue-600 text-blue-700' : 'border-transparent text-slate-500 hover:text-slate-700'
            }`}
          >
            {t}
          </button>
        ))}
      </div>

      {tab === 'apariencia' ? <AppearanceEditor companyId={id} /> : <ProductsEditor companyId={id} />}
    </div>
  )
}
