import { axiosInstance } from '@/shared/api'

import type { OperatorSummary } from '../model/types'

/** Lists every operator (SystemAdmin only). */
export async function listOperators(): Promise<OperatorSummary[]> {
  const { data } = await axiosInstance.get<OperatorSummary[]>('/api/operators')
  return data
}

/** Creates a company operator. */
export async function createOperator(
  username: string,
  password: string,
  companyId: string,
): Promise<OperatorSummary> {
  const { data } = await axiosInstance.post<OperatorSummary>('/api/operators', {
    username,
    password,
    companyId,
  })
  return data
}
