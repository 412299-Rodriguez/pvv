import { useEffect } from 'react'
import { NavLink, Outlet } from 'react-router-dom'

import { getCompany } from '@/entities/company'
import { useSessionStore } from '@/entities/session'

const OPERATOR_TABS = [
  { to: '/dashboard', label: 'Dashboard' },
  { to: '/leads', label: 'Leads' },
  { to: '/apariencia', label: 'Apariencia' },
  { to: '/productos', label: 'Productos' },
  { to: '/recupero', label: 'Recupero' },
]
const ADMIN_TABS = [
  { to: '/companias', label: 'Compañías' },
  { to: '/operadores', label: 'Operadores' },
]

/**
 * Authenticated layout.
 *
 * The bar is a rule with things on it, not a raised white slab: it shares the
 * page's paper and is separated by a hairline. Navigation is plain text with an
 * underline on the active item — a tinted pill per tab is the loudest way to
 * say something a 2px rule says quietly.
 */
export function AppShell() {
  const role = useSessionStore((s) => s.role)
  const username = useSessionStore((s) => s.username)
  const companyId = useSessionStore((s) => s.companyId)
  const companyName = useSessionStore((s) => s.companyName)
  const setCompanyName = useSessionStore((s) => s.setCompanyName)
  const logout = useSessionStore((s) => s.logout)

  const tabs = role === 'SystemAdmin' ? ADMIN_TABS : OPERATOR_TABS

  // An operator works inside one company, so the shell says which one. Loaded
  // once per session; a SystemAdmin has none and picks a company per screen.
  useEffect(() => {
    if (role !== 'CompanyOperator' || !companyId || companyName) return

    let active = true
    getCompany(companyId)
      .then((company) => active && setCompanyName(company.name))
      .catch(() => {})
    return () => {
      active = false
    }
  }, [role, companyId, companyName, setCompanyName])

  return (
    <div className="min-h-screen">
      <header className="sticky top-0 z-20 border-b border-stone-200/80 bg-[#f6f5f2]/85 backdrop-blur">
        <div className="mx-auto flex h-14 max-w-6xl items-stretch gap-8 px-6">
          <div className="flex items-center gap-2.5">
            <span className="flex h-7 w-7 items-center justify-center rounded bg-stone-900 text-[10px] font-bold tracking-tight text-white">
              PVV
            </span>
            {/* The tenant being worked on is the useful half of the title. */}
            <span className="text-sm font-semibold tracking-tight text-stone-900">
              {companyName ?? 'Administración'}
            </span>
          </div>

          <nav className="flex items-stretch gap-6">
            {tabs.map((tab) => (
              <NavLink key={tab.to} to={tab.to} className="group relative flex items-center">
                {({ isActive }) => (
                  <>
                    <span
                      className={`text-sm transition ${
                        isActive
                          ? 'font-medium text-stone-900'
                          : 'text-stone-500 group-hover:text-stone-900'
                      }`}
                    >
                      {tab.label}
                    </span>
                    {isActive ? (
                      <span className="absolute inset-x-0 -bottom-px h-0.5 rounded-full bg-stone-900" />
                    ) : null}
                  </>
                )}
              </NavLink>
            ))}
          </nav>

          <div className="ml-auto flex items-center gap-4">
            <span className="hidden text-xs text-stone-500 sm:inline">{username}</span>
            <button
              type="button"
              onClick={logout}
              className="text-xs font-medium text-stone-500 underline-offset-4 transition hover:text-stone-900 hover:underline"
            >
              Salir
            </button>
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-6xl px-6 py-10">
        <Outlet />
      </main>
    </div>
  )
}
