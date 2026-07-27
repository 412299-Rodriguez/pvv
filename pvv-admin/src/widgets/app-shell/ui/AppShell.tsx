import { NavLink, Outlet } from 'react-router-dom'

import { useSessionStore } from '@/entities/session'

const OPERATOR_TABS = [
  { to: '/dashboard', label: 'Dashboard' },
  { to: '/leads', label: 'Leads' },
  { to: '/apariencia', label: 'Apariencia' },
  { to: '/productos', label: 'Productos' },
]
const ADMIN_TABS = [
  { to: '/companias', label: 'Compañías' },
  { to: '/operadores', label: 'Operadores' },
]

/** Authenticated layout: top bar with role-based tabs + the active page. */
export function AppShell() {
  const role = useSessionStore((s) => s.role)
  const username = useSessionStore((s) => s.username)
  const logout = useSessionStore((s) => s.logout)

  const tabs = role === 'SystemAdmin' ? ADMIN_TABS : OPERATOR_TABS

  return (
    <div className="min-h-screen bg-slate-100">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-3">
          <div className="flex items-center gap-8">
            <span className="text-lg font-bold text-slate-800">PVV Admin</span>
            <nav className="flex gap-1">
              {tabs.map((tab) => (
                <NavLink
                  key={tab.to}
                  to={tab.to}
                  className={({ isActive }) =>
                    `rounded-lg px-3 py-2 text-sm font-semibold transition ${
                      isActive ? 'bg-blue-50 text-blue-700' : 'text-slate-600 hover:bg-slate-100'
                    }`
                  }
                >
                  {tab.label}
                </NavLink>
              ))}
            </nav>
          </div>

          <div className="flex items-center gap-4">
            <span className="text-sm text-slate-500">
              {username} · <span className="font-medium text-slate-700">
                {role === 'SystemAdmin' ? 'Administrador' : 'Operador'}
              </span>
            </span>
            <button
              type="button"
              onClick={logout}
              className="rounded-lg border border-slate-300 px-3 py-1.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-100"
            >
              Salir
            </button>
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-6xl px-6 py-8">
        <Outlet />
      </main>
    </div>
  )
}
