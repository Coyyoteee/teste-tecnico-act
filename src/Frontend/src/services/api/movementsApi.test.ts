import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  ApiError,
  createMovement,
  getBalance,
  getMovements,
} from './movementsApi'

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('movementsApi', () => {
  it('loads the balance from the contract endpoint', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ balance: 125.5 }))
    vi.stubGlobal('fetch', fetchMock)

    await expect(getBalance()).resolves.toEqual({ balance: 125.5 })
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/balance', undefined)
  })

  it('loads movements preserving the API order', async () => {
    const movements = [movement('newer', 'debit'), movement('older', 'credit')]
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(jsonResponse(movements)))

    await expect(getMovements()).resolves.toEqual(movements)
  })

  it('creates a movement with the expected payload', async () => {
    const created = movement('created', 'credit')
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(created, 201))
    vi.stubGlobal('fetch', fetchMock)

    await expect(createMovement({ type: 'credit', amount: 10.5 })).resolves.toEqual(created)
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/movements', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ type: 'credit', amount: 10.5 }),
    })
  })

  it.each([400, 409, 500])('throws ApiError for status %s', async (status) => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse({ status, title: 'Problem' }, status)),
    )

    const error = await createMovement({ type: 'debit', amount: 100 }).catch((reason) => reason)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status, problem: { status, title: 'Problem' } })
  })

  it('propagates network failures', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    await expect(getBalance()).rejects.toThrow('Failed to fetch')
  })
})

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': status >= 400 ? 'application/problem+json' : 'application/json' },
  })
}

function movement(id: string, type: 'credit' | 'debit') {
  return { id, type, amount: 10, occurredAt: '2026-08-26T15:30:00Z' }
}
