import { axiosInstance } from '@/shared/api'

import type { Role } from '../model/types'

export interface LoginResponse {
  token: string
  expiresAt: string
  companyId: string | null
  role: Role
}

/** Authenticates against pvv-config and returns the token + role + companyId. */
export async function login(username: string, password: string): Promise<LoginResponse> {
  const { data } = await axiosInstance.post<LoginResponse>('/api/auth/login', { username, password })
  return data
}
