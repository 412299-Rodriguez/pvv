import { Navigate } from 'react-router-dom'
import type { ReactNode } from 'react'

import { useSessionStore } from '@/entities/session'
import type { Role } from '@/entities/session'
import { AppShell } from '@/widgets/app-shell'

/** Auth gate + shell layout for the authenticated app. */
export function ProtectedLayout() {
  const token = useSessionStore((s) => s.token)
  if (!token) return <Navigate to="/login" replace />
  return <AppShell />
}

/** Sends the user to their role's home screen. */
export function HomeRedirect() {
  const role = useSessionStore((s) => s.role)
  return <Navigate to={role === 'SystemAdmin' ? '/companias' : '/apariencia'} replace />
}

/** Restricts a route to a single role; otherwise bounces to the role's home. */
export function RoleRoute({ allow, children }: { allow: Role; children: ReactNode }) {
  const role = useSessionStore((s) => s.role)
  if (role !== allow) return <HomeRedirect />
  return <>{children}</>
}
