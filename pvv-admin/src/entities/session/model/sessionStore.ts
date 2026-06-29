import { create } from 'zustand'

import type { Role } from './types'

const TOKEN_KEY = 'pvv-admin-token'
const SESSION_KEY = 'pvv-admin-session'

interface PersistedSession {
  role: Role
  companyId: string | null
  username: string
}

interface SessionState {
  token: string | null
  role: Role | null
  companyId: string | null
  username: string | null
  setSession: (data: { token: string; role: Role; companyId: string | null; username: string }) => void
  logout: () => void
}

function loadPersisted(): PersistedSession | null {
  try {
    const raw = localStorage.getItem(SESSION_KEY)
    return raw ? (JSON.parse(raw) as PersistedSession) : null
  } catch {
    return null
  }
}

const persisted = loadPersisted()

/**
 * Operator session. The token is mirrored to `pvv-admin-token` (read by the axios
 * interceptor) and the rest to `pvv-admin-session`, so a refresh keeps the login.
 */
export const useSessionStore = create<SessionState>((set) => ({
  token: localStorage.getItem(TOKEN_KEY),
  role: persisted?.role ?? null,
  companyId: persisted?.companyId ?? null,
  username: persisted?.username ?? null,

  setSession: ({ token, role, companyId, username }) => {
    localStorage.setItem(TOKEN_KEY, token)
    localStorage.setItem(SESSION_KEY, JSON.stringify({ role, companyId, username }))
    set({ token, role, companyId, username })
  },

  logout: () => {
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(SESSION_KEY)
    set({ token: null, role: null, companyId: null, username: null })
  },
}))
