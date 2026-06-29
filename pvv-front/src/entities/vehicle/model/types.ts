/** A vehicle that can be insured. */
export interface Vehicle {
  make: string;
  model: string;
  year: number;
  /** Display plate, e.g. "ABC 123". */
  plate: string;
}

/** Human-readable one-liner: "Toyota Corolla 2022". */
export function vehicleTitle(vehicle: Vehicle): string {
  return `${vehicle.make} ${vehicle.model} ${vehicle.year}`;
}
