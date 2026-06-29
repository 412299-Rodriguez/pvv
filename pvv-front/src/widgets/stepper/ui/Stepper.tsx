import { useSessionStore } from '@/entities/session';
import { CheckIcon } from '@/shared/ui/icons';
import { STAGES, getStepperView } from '../model/stepperStages';
import styles from './Stepper.module.css';

/**
 * Sticky top stepper. Completed stages are clickable to navigate back.
 */
export function Stepper() {
  const step = useSessionStore((s) => s.step);
  const goTo = useSessionStore((s) => s.goTo);
  const view = getStepperView(step);

  return (
    <div className={styles.wrap}>
      <div className={styles.stepper}>
        <div className={styles.line}>
          <i style={{ width: `${view.progress}%` }} />
        </div>

        {STAGES.map((stage, index) => {
          const isActive = index === view.activeIndex;
          const isDone = view.doneIndices.includes(index);
          const Icon = stage.icon;

          return (
            <div
              key={stage.key}
              className={[
                styles.stp,
                isActive ? styles.active : '',
                isDone ? styles.done : '',
                isDone ? styles.clickable : '',
              ]
                .filter(Boolean)
                .join(' ')}
              onClick={() => {
                if (isDone) goTo(stage.target);
              }}
            >
              <div className={styles.dot}>
                {isDone ? <CheckIcon className={styles.check} /> : <Icon />}
              </div>
              <div className={styles.label}>{stage.label}</div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
