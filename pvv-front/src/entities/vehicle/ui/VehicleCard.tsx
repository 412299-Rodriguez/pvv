import { CarIcon, CheckIcon } from '@/shared/ui/icons';
import { type Vehicle, vehicleTitle } from '../model/types';
import styles from './VehicleCard.module.css';

interface VehicleCardProps {
  vehicle: Vehicle;
  /** Show the green "Verificado" pill. Default true. */
  verified?: boolean;
}

/**
 * Modern summary card for a looked-up vehicle: gradient icon tile, model name
 * and a plate chip, with an optional "verified" badge.
 */
export function VehicleCard({ vehicle, verified = true }: VehicleCardProps) {
  return (
    <div className={styles.card}>
      <div className={styles.tile}>
        <CarIcon />
      </div>
      <div className={styles.body}>
        <div className={styles.eyebrow}>Tu vehículo</div>
        <div className={styles.name}>{vehicleTitle(vehicle)}</div>
        <div className={styles.plateRow}>
          <span className={styles.plate}>{vehicle.plate}</span>
          {verified && (
            <span className={styles.pill}>
              <CheckIcon />
              Verificado
            </span>
          )}
        </div>
      </div>
    </div>
  );
}
