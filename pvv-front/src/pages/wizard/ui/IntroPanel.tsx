import { Card, BoltIcon, MobileIcon, LockIcon } from '@/shared/ui';
import styles from './IntroPanel.module.css';

interface Feature {
  icon: typeof BoltIcon;
  title: string;
  description: string;
}

const FEATURES: Feature[] = [
  { icon: BoltIcon, title: 'Emisión inmediata', description: 'Tu póliza en segundos' },
  { icon: MobileIcon, title: '100% online', description: 'Sin turnos ni papeles' },
  { icon: LockIcon, title: 'Pago seguro', description: 'Encriptado y protegido' },
];

/** Marketing / value-proposition panel shown beside the plate form on step 1. */
export function IntroPanel() {
  return (
    <Card>
      <h1 className={styles.title}>Cotizá tu seguro en minutos</h1>
      <p className={styles.subtitle}>100% digital, sin papeles, sin filas.</p>

      <div className={styles.features}>
        {FEATURES.map(({ icon: Icon, title, description }) => (
          <div key={title} className={styles.feature}>
            <span className={styles.featureIcon}>
              <Icon />
            </span>
            <div className={styles.featureText}>
              <b>{title}</b>
              <span>{description}</span>
            </div>
          </div>
        ))}
      </div>

      <div className={styles.trust}>
        <span className={styles.stars}>★★★★★</span> Miles de pólizas emitidas
      </div>
    </Card>
  );
}
