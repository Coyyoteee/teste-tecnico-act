import { formatCurrency } from '../../utils/formatters'
import styles from './BalanceCard.module.css'

interface BalanceCardProps {
  balance: number | null
  isLoading: boolean
}

export function BalanceCard({ balance, isLoading }: BalanceCardProps) {
  return (
    <section className={styles.card} aria-labelledby="balance-title">
      <p className={styles.label} id="balance-title">Saldo disponível</p>
      {isLoading && balance === null ? (
        <p className={styles.loading}>Carregando saldo…</p>
      ) : (
        <p className={styles.value}>{formatCurrency(balance ?? 0)}</p>
      )}
      <p className={styles.note}>Atualizado a partir das movimentações registradas</p>
    </section>
  )
}
