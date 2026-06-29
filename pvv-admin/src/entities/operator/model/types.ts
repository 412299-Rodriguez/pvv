export interface OperatorSummary {
  operatorId: string
  username: string
  companyId: string | null
  companyName: string | null
  role: string
  isActive: boolean
  createdAt: string
}
