import { useEffect, useState, type FormEvent } from 'react'

import {
  createOperator,
  deleteOperator,
  listOperators,
  updateOperator,
  type OperatorSummary,
} from '@/entities/operator'
import { listCompanies, type Company } from '@/entities/company'
import { Button, Card, Field, inputClass } from '@/shared/ui'

export function OperatorsPage() {
  const [operators, setOperators] = useState<OperatorSummary[]>([])
  const [companies, setCompanies] = useState<Company[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [companyId, setCompanyId] = useState('')
  const [creating, setCreating] = useState(false)

  const [editingId, setEditingId] = useState<string | null>(null)
  const [editCompanyId, setEditCompanyId] = useState('')
  const [editActive, setEditActive] = useState(true)
  const [editPassword, setEditPassword] = useState('')

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

  const startEdit = (op: OperatorSummary) => {
    setEditingId(op.operatorId)
    setEditCompanyId(op.companyId ?? companies[0]?.companyId ?? '')
    setEditActive(op.isActive)
    setEditPassword('')
  }

  const saveEdit = async () => {
    if (!editingId) return
    setError(null)
    try {
      await updateOperator(editingId, {
        companyId: editCompanyId,
        isActive: editActive,
        password: editPassword,
      })
      setEditingId(null)
      load()
    } catch {
      setError('No se pudo guardar el operador.')
    }
  }

  const remove = async (op: OperatorSummary) => {
    if (!window.confirm(`¿Borrar al operador "${op.username}"?`)) return
    setError(null)
    try {
      await deleteOperator(op.operatorId)
      load()
    } catch {
      setError('No se pudo borrar el operador.')
    }
  }

  // Only company operators are editable here (the SystemAdmin row is read-only).
  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold tracking-tight text-stone-900">Operadores</h1>

      <Card>
        <h2 className="mb-3 font-bold text-stone-800">Nuevo operador de compañía</h2>
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
      </Card>

      {error && <p className="text-sm font-medium text-red-600">{error}</p>}

      <Card>
        {loading ? (
          <p className="text-sm text-stone-500">Cargando…</p>
        ) : (
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-stone-200 text-stone-500">
                <th className="py-2">Usuario</th>
                <th className="py-2">Compañía</th>
                <th className="py-2">Rol</th>
                <th className="py-2">Estado</th>
                <th className="py-2 text-right">Acciones</th>
              </tr>
            </thead>
            <tbody>
              {operators.map((op) => {
                const isAdmin = op.role === 'SystemAdmin'
                if (editingId === op.operatorId) {
                  return (
                    <tr key={op.operatorId} className="border-b border-stone-100 bg-stone-50">
                      <td className="py-2 pr-2 font-semibold text-stone-800">{op.username}</td>
                      <td className="py-2 pr-2">
                        <select
                          value={editCompanyId}
                          onChange={(e) => setEditCompanyId(e.target.value)}
                          className={inputClass}
                        >
                          {companies.map((c) => (
                            <option key={c.companyId} value={c.companyId}>
                              {c.name}
                            </option>
                          ))}
                        </select>
                      </td>
                      <td className="py-2 pr-2">
                        <input
                          type="password"
                          value={editPassword}
                          onChange={(e) => setEditPassword(e.target.value)}
                          className={inputClass}
                          placeholder="Nueva contraseña (opcional)"
                        />
                      </td>
                      <td className="py-2">
                        <label className="flex items-center gap-2 text-sm text-stone-700">
                          <input type="checkbox" checked={editActive} onChange={(e) => setEditActive(e.target.checked)} />
                          Activo
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
                  )
                }
                return (
                  <tr key={op.operatorId} className="border-b border-stone-100">
                    <td className="py-3 font-semibold text-stone-800">{op.username}</td>
                    <td className="py-3 text-stone-600">{op.companyName ?? '—'}</td>
                    <td className="py-3 text-stone-600">{isAdmin ? 'Administrador' : 'Operador'}</td>
                    <td className="py-3">
                      <span
                        className={`rounded-full px-2 py-0.5 text-xs font-semibold ${
                          op.isActive ? 'bg-green-100 text-green-700' : 'bg-stone-100 text-stone-500'
                        }`}
                      >
                        {op.isActive ? 'Activo' : 'Inactivo'}
                      </span>
                    </td>
                    <td className="py-3">
                      <div className="flex items-center justify-end gap-3">
                        {isAdmin ? (
                          <span className="text-xs text-stone-400">—</span>
                        ) : (
                          <>
                            <Button variant="secondary" onClick={() => startEdit(op)}>
                              Editar
                            </Button>
                            <Button variant="danger" onClick={() => remove(op)}>
                              Borrar
                            </Button>
                          </>
                        )}
                      </div>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        )}
      </Card>
    </div>
  )
}
