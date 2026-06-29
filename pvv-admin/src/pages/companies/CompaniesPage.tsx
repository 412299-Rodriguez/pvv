import { useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'

import { createCompany, listCompanies, type Company } from '@/entities/company'
import { Button, Card, Field, inputClass } from '@/shared/ui'

export function CompaniesPage() {
  const [companies, setCompanies] = useState<Company[]>([])
  const [loading, setLoading] = useState(true)
  const [name, setName] = useState('')
  const [cuit, setCuit] = useState('')
  const [creating, setCreating] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = () => {
    setLoading(true)
    listCompanies()
      .then((data) => {
        setCompanies(data)
        setLoading(false)
      })
      .catch(() => setLoading(false))
  }

  useEffect(() => {
    load()
  }, [])

  const handleCreate = async (event: FormEvent) => {
    event.preventDefault()
    setCreating(true)
    setError(null)
    try {
      await createCompany(name, cuit)
      setName('')
      setCuit('')
      load()
    } catch {
      setError('No se pudo crear la compañía (¿CUIT duplicado?).')
    } finally {
      setCreating(false)
    }
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-slate-800">Compañías</h1>

      <Card>
        <h2 className="mb-3 font-bold text-slate-800">Nueva compañía</h2>
        <form onSubmit={handleCreate} className="flex flex-wrap items-end gap-3">
          <div className="min-w-48 flex-1">
            <Field label="Nombre">
              <input value={name} onChange={(e) => setName(e.target.value)} className={inputClass} required />
            </Field>
          </div>
          <div className="min-w-48 flex-1">
            <Field label="CUIT">
              <input
                value={cuit}
                onChange={(e) => setCuit(e.target.value)}
                className={inputClass}
                placeholder="30-12345678-9"
                required
              />
            </Field>
          </div>
          <Button type="submit" disabled={creating || !name || !cuit}>
            {creating ? 'Creando…' : 'Crear compañía'}
          </Button>
        </form>
        {error && <p className="mt-2 text-sm font-medium text-red-600">{error}</p>}
      </Card>

      <Card>
        {loading ? (
          <p className="text-sm text-slate-500">Cargando…</p>
        ) : companies.length === 0 ? (
          <p className="text-sm text-slate-400">No hay compañías.</p>
        ) : (
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-slate-200 text-slate-500">
                <th className="py-2">Nombre</th>
                <th className="py-2">CUIT</th>
                <th className="py-2">Estado</th>
                <th className="py-2"></th>
              </tr>
            </thead>
            <tbody>
              {companies.map((company) => (
                <tr key={company.companyId} className="border-b border-slate-100">
                  <td className="py-3 font-semibold text-slate-800">{company.name}</td>
                  <td className="py-3 text-slate-600">{company.cuit}</td>
                  <td className="py-3">
                    <span
                      className={`rounded-full px-2 py-0.5 text-xs font-semibold ${
                        company.isActive ? 'bg-green-100 text-green-700' : 'bg-slate-100 text-slate-500'
                      }`}
                    >
                      {company.isActive ? 'Activa' : 'Inactiva'}
                    </span>
                  </td>
                  <td className="py-3 text-right">
                    <Link
                      to={`/companias/${company.companyId}`}
                      className="font-semibold text-blue-600 hover:underline"
                    >
                      Configurar →
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>
    </div>
  )
}
