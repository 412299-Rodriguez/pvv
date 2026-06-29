import { useEffect, useState, type FormEvent } from 'react'

import { createOperator, listOperators, type OperatorSummary } from '@/entities/operator'
import { listCompanies, type Company } from '@/entities/company'
import { Button, Card, Field, inputClass } from '@/shared/ui'

export function OperatorsPage() {
  const [operators, setOperators] = useState<OperatorSummary[]>([])
  const [companies, setCompanies] = useState<Company[]>([])
  const [loading, setLoading] = useState(true)

  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [companyId, setCompanyId] = useState('')
  const [creating, setCreating] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = () => {
    setLoading(true)
    Promise.all([listOperators(), listCompanies()])
      .then(([ops, cos]) => {
        setOperators(ops)
        setCompanies(cos)
        setCompanyId((current) => current || cos[0]?.companyId || '')
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
      await createOperator(username, password, companyId)
      setUsername('')
      setPassword('')
      load()
    } catch {
      setError('No se pudo crear el operador (¿usuario duplicado?).')
    } finally {
      setCreating(false)
    }
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-slate-800">Operadores</h1>

      <Card>
        <h2 className="mb-3 font-bold text-slate-800">Nuevo operador de compañía</h2>
        <form onSubmit={handleCreate} className="flex flex-wrap items-end gap-3">
          <div className="min-w-44 flex-1">
            <Field label="Usuario">
              <input
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                className={inputClass}
                placeholder="operador@empresa.com"
                required
              />
            </Field>
          </div>
          <div className="min-w-40 flex-1">
            <Field label="Contraseña">
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className={inputClass}
                required
              />
            </Field>
          </div>
          <div className="min-w-44 flex-1">
            <Field label="Compañía">
              <select value={companyId} onChange={(e) => setCompanyId(e.target.value)} className={inputClass} required>
                {companies.map((c) => (
                  <option key={c.companyId} value={c.companyId}>
                    {c.name}
                  </option>
                ))}
              </select>
            </Field>
          </div>
          <Button type="submit" disabled={creating || !username || !password || !companyId}>
            {creating ? 'Creando…' : 'Crear operador'}
          </Button>
        </form>
        {error && <p className="mt-2 text-sm font-medium text-red-600">{error}</p>}
      </Card>

      <Card>
        {loading ? (
          <p className="text-sm text-slate-500">Cargando…</p>
        ) : (
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-slate-200 text-slate-500">
                <th className="py-2">Usuario</th>
                <th className="py-2">Compañía</th>
                <th className="py-2">Rol</th>
                <th className="py-2">Estado</th>
              </tr>
            </thead>
            <tbody>
              {operators.map((op) => (
                <tr key={op.operatorId} className="border-b border-slate-100">
                  <td className="py-3 font-semibold text-slate-800">{op.username}</td>
                  <td className="py-3 text-slate-600">{op.companyName ?? '—'}</td>
                  <td className="py-3 text-slate-600">
                    {op.role === 'SystemAdmin' ? 'Administrador' : 'Operador'}
                  </td>
                  <td className="py-3">
                    <span
                      className={`rounded-full px-2 py-0.5 text-xs font-semibold ${
                        op.isActive ? 'bg-green-100 text-green-700' : 'bg-slate-100 text-slate-500'
                      }`}
                    >
                      {op.isActive ? 'Activo' : 'Inactivo'}
                    </span>
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
