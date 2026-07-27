import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'

import { login, useSessionStore } from '@/entities/session'
import { Button, Field, inputClass } from '@/shared/ui'

export function LoginPage() {
  const setSession = useSessionStore((s) => s.setSession)
  const navigate = useNavigate()

  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setLoading(true)
    setError(null)
    try {
      const result = await login(username, password)
      setSession({
        token: result.token,
        role: result.role,
        companyId: result.companyId,
        username,
      })
      navigate(result.role === 'SystemAdmin' ? '/companias' : '/dashboard', { replace: true })
    } catch {
      setError('Usuario o contraseña incorrectos.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center p-6">
      <div className="w-full max-w-sm">
        {/* The mark sits outside the form, left-aligned with it, so the card
            holds only the two things it is asking for. */}
        <div className="mb-8 flex items-center gap-3">
          <span className="flex h-9 w-9 items-center justify-center rounded bg-stone-900 text-[11px] font-bold tracking-tight text-white">
            PVV
          </span>
          <div>
            <div className="text-base font-semibold tracking-tight text-stone-900">
              Panel de administración
            </div>
            <div className="text-sm text-stone-500">Portal Venta Vehicular</div>
          </div>
        </div>

        <form
          onSubmit={handleSubmit}
          className="space-y-4 rounded-lg border border-stone-200 bg-white p-6"
        >
          <Field label="Usuario">
            <input
              type="text"
              autoComplete="username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              className={inputClass}
              placeholder="usuario@empresa.com"
            />
          </Field>

          <Field label="Contraseña">
            <input
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className={inputClass}
              placeholder="••••••••"
            />
          </Field>

          {error ? (
            <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>
          ) : null}

          <Button type="submit" disabled={loading || !username || !password} className="w-full">
            {loading ? 'Ingresando…' : 'Ingresar'}
          </Button>
        </form>
      </div>
    </div>
  )
}
