import { BalanceCard } from './components/BalanceCard/BalanceCard'
import { Feedback } from './components/Feedback/Feedback'
import { MovementForm } from './components/MovementForm/MovementForm'
import { MovementHistory } from './components/MovementHistory/MovementHistory'
import { useAccount } from './features/account/useAccount'
import styles from './App.module.css'

export function App() {
  const account = useAccount()
  const hasData = account.balance !== null

  return (
    <main className={styles.page}>
      <div className={styles.container}>
        <header className={styles.header}>
          <p className={styles.eyebrow}>Conta empresarial</p>
          <h1>Controle financeiro</h1>
          <p className={styles.introduction}>
            Registre entradas e saídas e acompanhe o saldo da conta em um só lugar.
          </p>
        </header>

        {account.loadError && !hasData ? (
          <section className={styles.loadError} aria-labelledby="load-error-title">
            <h2 id="load-error-title">Não foi possível carregar os dados</h2>
            <p>Verifique a conexão com o servidor e tente novamente.</p>
            <button type="button" onClick={() => void account.refresh()}>
              Tentar novamente
            </button>
          </section>
        ) : (
          <div className={styles.content}>
            <BalanceCard balance={account.balance} isLoading={account.isInitialLoading} />

            {account.loadError && hasData && (
              <Feedback
                kind="warning"
                message="Os dados exibidos podem estar desatualizados."
                actionLabel="Atualizar novamente"
                onAction={() => void account.refresh()}
              />
            )}

            {account.feedback && (
              <Feedback kind={account.feedback.kind} message={account.feedback.text} />
            )}

            <section className={styles.card} aria-labelledby="movement-form-title">
              <div className={styles.sectionHeading}>
                <p className={styles.sectionNumber} aria-hidden="true">01</p>
                <div>
                  <h2 id="movement-form-title">Nova movimentação</h2>
                  <p>Informe o tipo e o valor que deseja registrar.</p>
                </div>
              </div>
              <MovementForm
                isSubmitting={account.isSubmitting}
                onSubmit={account.submitMovement}
              />
            </section>

            <section className={styles.card} aria-labelledby="history-title">
              <div className={styles.sectionHeading}>
                <p className={styles.sectionNumber} aria-hidden="true">02</p>
                <div>
                  <h2 id="history-title">Histórico</h2>
                  <p>Movimentações mais recentes aparecem primeiro.</p>
                </div>
              </div>
              <MovementHistory
                movements={account.movements}
                isLoading={account.isInitialLoading}
              />
            </section>
          </div>
        )}
      </div>
    </main>
  )
}
