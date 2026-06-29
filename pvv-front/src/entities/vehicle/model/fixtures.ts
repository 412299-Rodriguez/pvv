import type { Vehicle } from './types';

/**
 * Demo vehicle returned by the (simulated) plate lookup.
 * In production this comes from a backend query by plate.
 */
export const demoVehicle: Vehicle = {
  make: 'Toyota',
  model: 'Corolla',
  year: 2022,
  plate: 'ABC 123',
};
