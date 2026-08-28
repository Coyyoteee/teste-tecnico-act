import { useState, type FormEvent } from 'react'
import * as ToggleGroup from '@radix-ui/react-toggle-group'
import type { CreateMovementRequest, MovementType } from '../../services/api/movementsApi'
import styles from './MovementForm.module.css'

interface MovementFormProps {
  isSubmitting: boolean
  onSubmit: (request: CreateMovementRequest) => Promise<boolean>
}

const maxAmountDigits = 15

function sanitizeAmount(value: string) {
  return value.replace(/\D/g, '').replace(/^0+/, '').slice(0, maxAmountDigits)
}

function formatAmount(cents: string) {
  const paddedAmount = cents.padStart(3, '0')
  const integerPart = paddedAmount.slice(0, -2).replace(/\B(?=(\d{3})+(?!\d))/g, '.')
  const decimalPart = paddedAmount.slice(-2)

  return `${integerPart},${decimalPart}`
}

export function MovementForm({ isSubmitting, onSubmit }: MovementFormProps) {
  const [type, setType] = useState<MovementType>('credit')
  const [amount, setAmount] = useState('')
  const amountInCents = Number(amount)
  const canSubmit = amount !== '' && amountInCents > 0 && !isSubmitting

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!event.currentTarget.checkValidity() || !canSubmit) return

    const created = await onSubmit({ type, amount: amountInCents / 100 })
    if (created) setAmount('')
  }

  return (
    <form className={styles.form} onSubmit={handleSubmit}>
      <fieldset className={styles.fieldset} disabled={isSubmitting}>
        <legend>Tipo de movimentação</legend>
        <ToggleGroup.Root
          className={styles.toggleGroup}
          type="single"
          value={type}
          aria-label="Tipo de movimentação"
          onValueChange={(value) => {
            if (value === 'credit' || value === 'debit') setType(value)
          }}
        >
          <ToggleGroup.Item className={styles.toggleItem} value="credit" aria-label="Entrada">
            <span aria-hidden="true">+</span> Entrada
          </ToggleGroup.Item>
          <ToggleGroup.Item className={styles.toggleItem} value="debit" aria-label="Saída">
            <span aria-hidden="true">−</span> Saída
          </ToggleGroup.Item>
        </ToggleGroup.Root>
      </fieldset>

      <div className={styles.amountField}>
        <label htmlFor="movement-amount">Valor</label>
        <div className={styles.inputWrapper}>
          <span aria-hidden="true">R$</span>
          <input
            id="movement-amount"
            name="amount"
            type="text"
            inputMode="numeric"
            required
            aria-describedby="amount-hint"
            value={formatAmount(amount)}
            disabled={isSubmitting}
            onChange={(event) => setAmount(sanitizeAmount(event.target.value))}
          />
        </div>
        <p className={styles.hint} id="amount-hint">Use valores maiores que zero.</p>
      </div>

      <button className={styles.submit} type="submit" disabled={!canSubmit}>
        {isSubmitting ? 'Registrando…' : 'Registrar movimentação'}
      </button>
    </form>
  )
}
