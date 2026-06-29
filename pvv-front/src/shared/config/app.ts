/**
 * App-wide configuration constants (structural values, no business entities).
 */

/** Insurer name shown across the portal. */
export const INSURER_NAME = 'Aseguradora Demo';

/**
 * Demo-only trigger: typing this plate at step 1 simulates a vehicle that
 * already has an active policy, opening the "renew early" modal.
 * In production this check would be a backend lookup.
 */
export const ACTIVE_POLICY_TRIGGER_PLATE = 'ABC123';

/** Milliseconds the fake policy emission takes before resolving. */
export const EMISSION_DURATION_MS = 3000;

/** Milliseconds the fake Mercado Pago redirect/processing spinner runs. */
export const PAYMENT_PROCESSING_MS = 1400;
