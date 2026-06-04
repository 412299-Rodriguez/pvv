import axios from 'axios'
import type { InternalAxiosRequestConfig } from 'axios'

/**
 * Central HTTP client for pvv-admin. Talks to pvv-config for CRUD operations.
 * The interceptor attaches the operator's JWT from localStorage.
 */
export const axiosInstance = axios.create({
  baseURL: import.meta.env.VITE_CONFIG_API_BASE_URL,
})

axiosInstance.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = localStorage.getItem('pvv-admin-token')

  if (token) {
    config.headers.set('Authorization', `Bearer ${token}`)
  }

  return config
})
