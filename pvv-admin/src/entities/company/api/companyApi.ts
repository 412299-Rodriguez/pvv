import { axiosInstance } from '@/shared/api'

import type { Company } from '../model/types'

/** Lists every company (SystemAdmin only). */
export async function listCompanies(): Promise<Company[]> {
  const { data } = await axiosInstance.get<Company[]>('/api/companies')
  return data
}

/** Gets a company by id. */
export async function getCompany(id: string): Promise<Company> {
  const { data } = await axiosInstance.get<Company>(`/api/companies/${id}`)
  return data
}

/** Creates a company (seeds its default UI config server-side). */
export async function createCompany(name: string, cuit: string): Promise<Company> {
  const { data } = await axiosInstance.post<Company>('/api/companies', { name, cuit })
  return data
}
