import { WizardPage } from '@/pages/wizard';
import styles from './App.module.css';

/**
 * Application root.
 *
 * The product is a single-page purchase wizard, so the app shell simply hosts
 * the wizard page. Cross-cutting providers (theme, i18n, error boundaries,
 * routing) would be composed here as the app grows.
 */
export function App() {
  return (
    <div className={styles.app}>
      <WizardPage />
    </div>
  );
}
