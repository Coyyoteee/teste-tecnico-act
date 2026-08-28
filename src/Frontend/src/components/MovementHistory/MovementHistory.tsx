import type { Movement } from '../../services/api/movementsApi'
import { formatCurrency, formatDateTime } from '../../utils/formatters'
import styles from './MovementHistory.module.css'

interface MovementHistoryProps {
  movements: Movement[]
  isLoading: boolean
}

export function MovementHistory({ movements, isLoading }: MovementHistoryProps) {
  if (isLoading && movements.length === 0) {
    return <p className={styles.empty}>Carregando histórico…</p>
  }

  if (movements.length === 0) {
    return <p className={styles.empty}>Nenhuma movimentação registrada.</p>
  }

  return (
    <ul className={styles.list}>
      {movements.map((movement) => {
        const isCredit = movement.type === 'credit'
        return (
          <li className={styles.item} key={movement.id}>
            <div className={`${styles.symbol} ${isCredit ? styles.credit : styles.debit}`} aria-hidden="true">
              {isCredit ? '+' : '−'}
            </div>
            <div className={styles.details}>
              <p className={styles.type}>{isCredit ? 'Entrada' : 'Saída'}</p>
              <time dateTime={movement.occurredAt}>{formatDateTime(movement.occurredAt)}</time>
            </div>
            <p className={`${styles.amount} ${isCredit ? styles.creditText : styles.debitText}`}>
              <span className={styles.visuallyHidden}>{isCredit ? 'Mais' : 'Menos'} </span>
              {isCredit ? '+' : '−'} {formatCurrency(movement.amount)}
            </p>
          </li>
        )
      })}
    </ul>
  )
}
