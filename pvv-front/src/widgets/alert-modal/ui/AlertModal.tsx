import { useAlertModalStore } from '@/shared/lib';
import { Button } from '@/shared/ui';
import styles from './AlertModal.module.css';

/** Friendly modal for surfacing errors/alerts (instead of inline in a card). */
export function AlertModal() {
  const alert = useAlertModalStore((s) => s.alert);
  const close = useAlertModalStore((s) => s.closeAlert);

  if (!alert) return null;

  return (
    <div
      className={styles.overlay}
      role="alertdialog"
      aria-modal="true"
      aria-label={alert.title}
      onClick={(e) => {
        if (e.target === e.currentTarget) close();
      }}
    >
      <div className={styles.modal}>
        <div className={styles.head}>
          <div className={styles.icon} aria-hidden>
            ⚠️
          </div>
          <h3 className={styles.title}>{alert.title}</h3>
        </div>

        {alert.detail && <p className={styles.copy}>{alert.detail}</p>}

        <div className={styles.buttons}>
          <Button variant="primary" fullWidth onClick={close}>
            Entendido
          </Button>
        </div>
      </div>
    </div>
  );
}
