export type CasePriority = 'Low' | 'Normal' | 'High' | 'Critical'
export type CaseStatus = 'New' | 'InProgress' | 'Resolved' | 'Closed'

export interface CaseItem {
  id: string
  caseNumber: string
  title: string
  description: string
  priority: CasePriority
  status: CaseStatus
  assignedUserId: string | null
  createdAt: string
  updatedAt: string
  version: number
}

export interface CreateCaseInput {
  title: string
  description: string
  priority: CasePriority
  assignedUserId: string | null
}

export interface ProblemDetails {
  title?: string
  detail?: string
}

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5080'

async function request<T>(path: string, persona: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      'X-MatterHarbor-User': persona,
      ...init?.headers,
    },
  })

  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ProblemDetails
    throw new Error(problem.detail ?? problem.title ?? `Request failed (${response.status})`)
  }

  return (await response.json()) as T
}

export const api = {
  listCases: (persona: string) => request<CaseItem[]>('/api/cases?page=1&pageSize=50', persona),
  getCase: (persona: string, id: string) => request<CaseItem>(`/api/cases/${id}`, persona),
  createCase: (persona: string, input: CreateCaseInput, idempotencyKey: string) =>
    request<CaseItem>('/api/cases', persona, {
      method: 'POST',
      headers: { 'Idempotency-Key': idempotencyKey },
      body: JSON.stringify(input),
    }),
}
