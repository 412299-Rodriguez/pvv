import type { ExistingPolicy } from './types';

/** Demo active policy used by the "renew early" flow. */
export const demoExistingPolicy: ExistingPolicy = {
  number: 'PVV-2025-000042',
  validUntil: '15/08/2026',
  earliestRenewalIso: '2026-08-16',
};

/** Generate a plausible new policy number: "PVV-2026-001234". */
export function generatePolicyNumber(year = new Date().getFullYear()): string {
  const serial = String(Math.floor(Math.random() * 900000) + 100000).padStart(6, '0');
  return `PVV-${year}-${serial}`;
}
