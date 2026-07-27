import axios from 'axios'
import type { InternalAxiosRequestConfig } from 'axios'

const TOKEN_KEY = 'pvv-admin-token'
const SESSION_KEY = 'pvv-admin-session'

/**
 * Builds an HTTP client that speaks for the logged-in operator: the interceptor
 * attaches the JWT from localStorage, and a 401 clears the session and bounces
 * to /login. The same pvv-config token is accepted by both back ends.
 */
function createClient(baseURL: string | undefined) {
  // exactOptionalPropertyTypes: pass the option only when it actually has a value.
  const client = axios.create(baseURL ? { baseURL } : {})

  client.interceptors.request.use((config: InternalAxiosRequestConfig) => {
    const token = localStorage.getItem(TOKEN_KEY)
    if (token) {
      config.headers.set('Authorization', `Bearer ${token}`)
    }
    return config
  })

  client.interceptors.response.use(
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

  return client
}

/** pvv-config — company / product / pricing / appearance CRUD. */
export const axiosInstance = createClient(import.meta.env.VITE_CONFIG_API_BASE_URL)

/**
 * pvv-bff — analytics. The leads live in the BFF's MongoDB, so the dashboard
 * reads them straight from there rather than through pvv-config.
 */
export const bffInstance = createClient(import.meta.env.VITE_BFF_BASE_URL)
