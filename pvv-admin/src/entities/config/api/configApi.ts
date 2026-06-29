import axios from 'axios'

import { axiosInstance } from '@/shared/api'

interface ConfigurationDto {
  configurationId: string
  companyId: string | null
  configurationType: string
  configurationValue: string
  version: number
  updatedAt: string
}

/**
 * Reads a company's config blob. The value is a JSON string we parse into T.
 * A 404 (config not created yet for that company) returns the fallback.
 */
export async function getConfig<T>(companyId: string, type: string, fallback: T): Promise<T> {
  try {
    const { data } = await axiosInstance.get<ConfigurationDto>(`/api/configurations/${companyId}/${type}`)
    return JSON.parse(data.configurationValue) as T
  } catch (error) {
    if (axios.isAxiosError(error) && error.response?.status === 404) {
      return fallback
    }
    throw error
  }
}

/** Upserts a company's config blob (free-form JSON) → propagates to the portal. */
export async function putConfig<T>(companyId: string, type: string, value: T): Promise<void> {
  await axiosInstance.put(`/api/configurations/${companyId}/${type}`, value)
}
