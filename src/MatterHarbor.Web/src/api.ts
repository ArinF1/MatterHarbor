export type CasePriority = 'Low' | 'Normal' | 'High' | 'Critical'
export type CaseStatus = 'New' | 'InProgress' | 'Resolved' | 'Closed'

export function nextCaseStatuses(status: CaseStatus): CaseStatus[] {
  switch (status) {
    case 'New': return ['InProgress']
    case 'InProgress': return ['Resolved']
    case 'Resolved': return ['InProgress', 'Closed']
    case 'Closed': return []
  }
}

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

export interface CaseAssignee {
  id: string
  displayName: string
}

export interface ProblemDetails {
  title?: string
  detail?: string
  type?: string
}

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5080'

export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly type?: string,
  ) {
    super(message)
  }
}

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
    throw new ApiError(
      problem.detail ?? problem.title ?? `Request failed (${response.status})`,
      response.status,
      problem.type,
    )
  }

  return (await response.json()) as T
}

export const api = {
  listCases: (persona: string) => request<CaseItem[]>('/api/cases?page=1&pageSize=50', persona),
  listAssignees: (persona: string) => request<CaseAssignee[]>('/api/cases/assignees', persona),
  getCase: (persona: string, id: string) => request<CaseItem>(`/api/cases/${id}`, persona),
  createCase: (persona: string, input: CreateCaseInput, idempotencyKey: string) =>
    request<CaseItem>('/api/cases', persona, {
      method: 'POST',
      headers: { 'Idempotency-Key': idempotencyKey },
      body: JSON.stringify(input),
    }),
  changeCaseStatus: (persona: string, id: string, status: CaseStatus, version: number, idempotencyKey: string) =>
    request<CaseItem>(`/api/cases/${id}/status`, persona, {
      method: 'PUT',
      headers: { 'If-Match': `"v${version}"`, 'Idempotency-Key': idempotencyKey },
      body: JSON.stringify({ status }),
    }),
  changeCaseAssignment: (persona: string, id: string, assignedUserId: string | null, version: number, idempotencyKey: string) =>
    request<CaseItem>(`/api/cases/${id}/assignment`, persona, {
      method: 'PUT',
      headers: { 'If-Match': `"v${version}"`, 'Idempotency-Key': idempotencyKey },
      body: JSON.stringify({ assignedUserId }),
    }),
}
