export type MovementType = 'credit' | 'debit'

export interface Movement {
  id: string
  type: MovementType
  amount: number
  occurredAt: string
}

export interface BalanceResponse {
  balance: number
}

export interface CreateMovementRequest {
  type: MovementType
  amount: number
}

export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
}

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly problem?: ProblemDetails,
  ) {
    super(problem?.title ?? `API request failed with status ${status}`)
    this.name = 'ApiError'
  }
}

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, init)

  if (!response.ok) {
    let problem: ProblemDetails | undefined
    try {
      problem = (await response.json()) as ProblemDetails
    } catch {
      problem = undefined
    }

    throw new ApiError(response.status, problem)
  }

  return (await response.json()) as T
}

export function getBalance(): Promise<BalanceResponse> {
  return request<BalanceResponse>('/api/v1/balance')
}

export function getMovements(): Promise<Movement[]> {
  return request<Movement[]>('/api/v1/movements')
}

export function createMovement(movement: CreateMovementRequest): Promise<Movement> {
  return request<Movement>('/api/v1/movements', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(movement),
  })
}
