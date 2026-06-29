import styles from './InvalidPortal.module.css';

/**
 * Shown when the URL has no valid company token (`?c=`) — i.e. this is not a
 * real point of sale. Intentionally friendly and dead-simple, with no wizard.
 */
export function InvalidPortal() {
  return (
    <div className={styles.screen}>
      <div className={styles.card}>
        <div className={styles.emoji} aria-hidden>
          🙁
        </div>
        <h1 className={styles.title}>Este no es un portal válido</h1>
        <p className={styles.text}>
          El enlace que usaste no corresponde a un punto de venta activo. Revisá la
          dirección o pedile el enlace correcto a tu aseguradora.
        </p>
        <div className={styles.brand}>Portal de Ventas Virtual</div>
      </div>
    </div>
  );
}
