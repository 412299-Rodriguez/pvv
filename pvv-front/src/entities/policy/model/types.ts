/** A policy that is already active on a vehicle (blocks immediate purchase). */
export interface ExistingPolicy {
  number: string;
  /** Display date "dd/mm/yyyy". */
  validUntil: string;
  /** ISO date (yyyy-mm-dd) of the earliest valid renewal start. */
  earliestRenewalIso: string;
}

/** A freshly issued policy shown on the success screen. */
export interface IssuedPolicy {
  number: string;
  vehicleTitle: string;
  holderName: string;
  /** Display date "dd/mm/yyyy". */
  validFrom: string;
  validUntil: string;
  pricePaid: number;
}
