import type { FeedbackKind } from '../../features/account/useAccount'
import styles from './Feedback.module.css'

interface FeedbackProps {
  kind: FeedbackKind
  message: string
  actionLabel?: string
  onAction?: () => void
}

export function Feedback({ kind, message, actionLabel, onAction }: FeedbackProps) {
  return (
    <div className={`${styles.feedback} ${styles[kind]}`} role={kind === 'error' ? 'alert' : 'status'}>
      <span className={styles.marker} aria-hidden="true" />
      <p>{message}</p>
      {actionLabel && onAction && (
        <button type="button" className={styles.action} onClick={onAction}>
          {actionLabel}
        </button>
      )}
    </div>
  )
}
