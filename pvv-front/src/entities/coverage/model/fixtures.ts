import type { Coverage } from './types';

/**
 * Catalog of available coverages. In production this comes from a quote API
 * keyed by vehicle + policyholder risk profile.
 */
export const availableCoverages: Coverage[] = [
  {
    id: 'basic',
    name: 'Cobertura Básica',
    coverageType: 'Responsabilidad Civil',
    pricePerYear: 45000,
    recommended: false,
    benefits: [
      'Responsabilidad civil',
      'Asistencia mecánica 24h',
      'Cobertura en todo el país',
    ],
  },
  {
    id: 'full',
    name: 'Cobertura Total',
    coverageType: 'Todo Riesgo',
    pricePerYear: 89000,
    recommended: true,
    benefits: [
      'Todo riesgo con franquicia',
      'Robo e incendio total/parcial',
      'Auto de reemplazo + grúa',
    ],
  },
];

/** Look up a coverage by id (null-safe). */
export function findCoverage(id: string | null): Coverage | undefined {
  if (!id) return undefined;
  return availableCoverages.find((c) => c.id === id);
}
