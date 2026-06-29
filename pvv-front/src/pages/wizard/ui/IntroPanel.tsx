import { Card, BoltIcon, MobileIcon, LockIcon } from '@/shared/ui';
import { useText, usePvvConfigStore } from '@/entities/company';
import styles from './IntroPanel.module.css';

/** Marketing / value-proposition panel shown beside the plate form on step 1. */
export function IntroPanel() {
  const logoUrl = usePvvConfigStore((s) => s.logoUrl);
  const companyName = usePvvConfigStore((s) => s.companyName);

  const introTitle = useText('introTitle', 'Cotizá tu seguro en minutos');
  const introSubtitle = useText('introSubtitle', '100% digital, sin papeles, sin filas.');
  const ratingText = useText('ratingText', 'Miles de pólizas emitidas');

  const feature1Title = useText('feature1Title', 'Emisión inmediata');
  const feature1Text = useText('feature1Text', 'Tu póliza en segundos');
  const feature2Title = useText('feature2Title', '100% online');
  const feature2Text = useText('feature2Text', 'Sin turnos ni papeles');
  const feature3Title = useText('feature3Title', 'Pago seguro');
  const feature3Text = useText('feature3Text', 'Encriptado y protegido');

  const features = [
    { icon: BoltIcon, title: feature1Title, description: feature1Text },
    { icon: MobileIcon, title: feature2Title, description: feature2Text },
    { icon: LockIcon, title: feature3Title, description: feature3Text },
  ];

  return (
    <Card className={styles.card}>
      <div className={styles.inner}>
        {(logoUrl || companyName) && (
          <div className={styles.brand}>
            {logoUrl && <img src={logoUrl} alt={companyName} className={styles.brandLogo} />}
            {companyName && <span className={styles.brandName}>{companyName}</span>}
          </div>
        )}

        <h1 className={styles.title}>{introTitle}</h1>
        <p className={styles.subtitle}>{introSubtitle}</p>

        <div className={styles.features}>
          {features.map(({ icon: Icon, title, description }) => (
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
          <span className={styles.stars}>★★★★★</span> {ratingText}
        </div>
      </div>
    </Card>
  );
}
