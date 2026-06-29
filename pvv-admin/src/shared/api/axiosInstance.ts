import axios from 'axios'
import type { InternalAxiosRequestConfig } from 'axios'

const TOKEN_KEY = 'pvv-admin-token'
const SESSION_KEY = 'pvv-admin-session'

/**
 * Central HTTP client for pvv-admin. Talks to pvv-config for CRUD operations.
 * The interceptor attaches the operator's JWT from localStorage; a 401 clears the
 * session and bounces to /login.
 */
export const axiosInstance = axios.create({
  baseURL: import.meta.env.VITE_CONFIG_API_BASE_URL,
})

axiosInstance.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = localStorage.getItem(TOKEN_KEY)
  if (token) {
    config.headers.set('Authorization', `Bearer ${token}`)
  }
  return config
})

axiosInstance.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error?.response?.status === 401) {
      localStorage.removeItem(TOKEN_KEY)
      localStorage.removeItem(SESSION_KEY)
      if (window.location.pathname !== '/login') {
        window.location.assign('/login')
      }
    }
    return Promise.reject(error)
  },
)
