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

/** Updates a company's editable fields. */
export async function updateCompany(
  id: string,
  changes: { name: string; cuit: string; isActive: boolean },
): Promise<Company> {
  const { data } = await axiosInstance.put<Company>(`/api/companies/${id}`, changes)
  return data
}

/** Deletes a company (and its configs + operators). */
export async function deleteCompany(id: string): Promise<void> {
  await axiosInstance.delete(`/api/companies/${id}`)
}
