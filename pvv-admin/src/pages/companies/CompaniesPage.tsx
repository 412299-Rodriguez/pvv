import { useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'

import {
  createCompany,
  deleteCompany,
  listCompanies,
  updateCompany,
  type Company,
} from '@/entities/company'
import { Button, Card, Field, inputClass } from '@/shared/ui'

export function CompaniesPage() {
  const [companies, setCompanies] = useState<Company[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [name, setName] = useState('')
  const [cuit, setCuit] = useState('')
  const [creating, setCreating] = useState(false)

  const [editingId, setEditingId] = useState<string | null>(null)
  const [editName, setEditName] = useState('')
  const [editCuit, setEditCuit] = useState('')
  const [editActive, setEditActive] = useState(true)

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

  const startEdit = (company: Company) => {
    setEditingId(company.companyId)
    setEditName(company.name)
    setEditCuit(company.cuit)
    setEditActive(company.isActive)
  }

  const saveEdit = async () => {
    if (!editingId) return
    setError(null)
    try {
      await updateCompany(editingId, { name: editName, cuit: editCuit, isActive: editActive })
      setEditingId(null)
      load()
    } catch {
      setError('No se pudo guardar la compañía.')
    }
  }

  const remove = async (company: Company) => {
    if (!window.confirm(`¿Borrar "${company.name}"? Se eliminan sus configuraciones y operadores.`)) return
    setError(null)
    try {
      await deleteCompany(company.companyId)
      load()
    } catch {
      setError('No se pudo borrar la compañía.')
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
      </Card>

      {error && <p className="text-sm font-medium text-red-600">{error}</p>}

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
                <th className="py-2 text-right">Acciones</th>
              </tr>
            </thead>
            <tbody>
              {companies.map((company) =>
                editingId === company.companyId ? (
                  <tr key={company.companyId} className="border-b border-slate-100 bg-slate-50">
                    <td className="py-2 pr-2">
                      <input value={editName} onChange={(e) => setEditName(e.target.value)} className={inputClass} />
                    </td>
                    <td className="py-2 pr-2">
                      <input value={editCuit} onChange={(e) => setEditCuit(e.target.value)} className={inputClass} />
                    </td>
                    <td className="py-2">
                      <label className="flex items-center gap-2 text-sm text-slate-700">
                        <input type="checkbox" checked={editActive} onChange={(e) => setEditActive(e.target.checked)} />
                        Activa
                      </label>
                    </td>
                    <td className="py-2 text-right">
                      <div className="flex justify-end gap-2">
                        <Button onClick={saveEdit}>Guardar</Button>
                        <Button variant="secondary" onClick={() => setEditingId(null)}>
                          Cancelar
                        </Button>
                      </div>
                    </td>
                  </tr>
                ) : (
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
                    <td className="py-3">
                      <div className="flex items-center justify-end gap-3">
                        <Link
                          to={`/companias/${company.companyId}`}
                          className="font-semibold text-blue-600 hover:underline"
                        >
                          Configurar
                        </Link>
                        <Button variant="secondary" onClick={() => startEdit(company)}>
                          Editar
                        </Button>
                        <Button variant="danger" onClick={() => remove(company)}>
                          Borrar
                        </Button>
                      </div>
                    </td>
                  </tr>
                ),
              )}
            </tbody>
          </table>
        )}
      </Card>
    </div>
  )
}
