import axios from 'axios'
import type { InternalAxiosRequestConfig } from 'axios'
import { requestContext, getClientId, SESSION_ID_KEY } from './requestContext'

/**
 * Central HTTP client for pvv-front. Every request goes through the BFF.
 * The interceptors attach the company token, anonymous session id, Turnstile
 * token and client id, and persist the session id the BFF returns.
 */
export const axiosInstance = axios.create({
  baseURL: import.meta.env.VITE_BFF_BASE_URL ?? 'http://localhost:5003',
})

axiosInstance.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const companyToken = requestContext.companyToken
  const sessionId = localStorage.getItem(SESSION_ID_KEY)
  const turnstileToken = requestContext.turnstileToken

  if (companyToken) {
    config.headers.set('X-Company-Token', companyToken)
  }
  if (sessionId) {
    config.headers.set('X-Session-Id', sessionId)
  }
  if (turnstileToken) {
    config.headers.set('X-Turnstile-Token', turnstileToken)
  }
  config.headers.set('X-Client-Id', getClientId())

  return config
})

// The BFF returns its (server-generated) session id in a header; persist it so
// later requests — including after a payment redirect — reuse the same session.
axiosInstance.interceptors.response.use((response) => {
  const sessionId = response.headers['x-session-id']
  if (typeof sessionId === 'string' && sessionId.length > 0) {
    localStorage.setItem(SESSION_ID_KEY, sessionId)
  }
  return response
})
