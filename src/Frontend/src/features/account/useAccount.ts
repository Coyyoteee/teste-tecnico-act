import { useCallback, useEffect, useState } from 'react'
import {
  ApiError,
  createMovement,
  getBalance,
  getMovements,
  type CreateMovementRequest,
  type Movement,
} from '../../services/api/movementsApi'

export type FeedbackKind = 'success' | 'error' | 'warning'

export interface FeedbackMessage {
  kind: FeedbackKind
  text: string
}

function operationErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 400) return 'Informe um tipo e um valor maior que zero.'
    if (error.status === 409) return 'Saldo insuficiente para realizar esta saída.'
    return 'Não foi possível concluir a operação. Tente novamente.'
  }

  return 'Não foi possível conectar ao servidor.'
}

export function useAccount() {
  const [balance, setBalance] = useState<number | null>(null)
  const [movements, setMovements] = useState<Movement[]>([])
  const [isInitialLoading, setIsInitialLoading] = useState(true)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [loadError, setLoadError] = useState(false)
  const [feedback, setFeedback] = useState<FeedbackMessage | null>(null)

  const refresh = useCallback(async () => {
    try {
      const [balanceResponse, history] = await Promise.all([getBalance(), getMovements()])
      setBalance(balanceResponse.balance)
      setMovements(history)
      setLoadError(false)
      return true
    } catch {
      setLoadError(true)
      return false
    } finally {
      setIsInitialLoading(false)
    }
  }, [])

  useEffect(() => {
    void refresh()
  }, [refresh])

  const submitMovement = useCallback(
    async (request: CreateMovementRequest) => {
      setIsSubmitting(true)
      setFeedback(null)
      try {
        await createMovement(request)
        const refreshed = await refresh()
        setFeedback(
          refreshed
            ? { kind: 'success', text: 'Movimentação registrada com sucesso.' }
            : {
                kind: 'warning',
                text: 'Movimentação registrada, mas não foi possível atualizar os dados.',
              },
        )
        return true
      } catch (error) {
        setFeedback({ kind: 'error', text: operationErrorMessage(error) })
        return false
      } finally {
        setIsSubmitting(false)
      }
    },
    [refresh],
  )

  return {
    balance,
    movements,
    isInitialLoading,
    isSubmitting,
    loadError,
    feedback,
    refresh,
    submitMovement,
  }
}
