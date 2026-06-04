import axios from 'axios'
import type { InternalAxiosRequestConfig } from 'axios'

/**
 * Central HTTP client for pvv-front. Every request goes through the BFF.
 * The interceptor attaches the company token and the anonymous session id.
 */
export const axiosInstance = axios.create({
  baseURL: import.meta.env.VITE_BFF_BASE_URL,
})

axiosInstance.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const companyToken = import.meta.env.VITE_COMPANY_TOKEN
  const sessionId = localStorage.getItem('pvv-session-id')

  if (companyToken) {
    config.headers.set('X-Company-Token', companyToken)
  }
  if (sessionId) {
    config.headers.set('X-Session-Id', sessionId)
  }

  return config
})
