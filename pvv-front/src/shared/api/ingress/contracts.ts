/**
 * Ingress wire contracts — the request/response shapes the front exchanges with
 * pvv-bff (the single entry point; see `../axiosInstance.ts`).
 *
 * These types are the STABLE boundary: in HU-05 they are served by mock data
 * (`./ingressClient.ts`), and in HU-10 the same signatures are fulfilled by real
 * HTTP calls — no caller has to change.
 *
 * TERMINOLOGY MAPPING (front ↔ backend):
 *   The person who holds the policy is modelled in the front as `Policyholder`
 *   (`entities/policyholder`). The pvv-soat / pvv-bff domain calls this same
 *   concept a **Holder**. To keep both vocabularies honest, the wire DTO is
 *   `HolderDto` and the mapping happens explicitly in the helpers below — never
 *   implicitly. Field shapes are identical today, but routing every conversion
 *   through these functions means a future divergence is a one-line change.
 */
import type { Vehicle } from '@/entities/vehicle';
import type { Coverage } from '@/entities/coverage';
import type { ExistingPolicy } from '@/entities/policy';
import type { DocumentType, Policyholder } from '@/entities/policyholder';

// ---- Plate check (step 1) --------------------------------------------------
export interface CheckPlateRequest {
  plate: string;
}
export interface CheckPlateResponse {
  vehicle: Vehicle;
  /** True when the plate already has an in-force policy (blocks new purchase). */
  hasActivePolicy: boolean;
  /** Present only when `hasActivePolicy` is true. */
  existingPolicy: ExistingPolicy | null;
}

// ---- Holder lookup (step 2) ------------------------------------------------
/** Wire shape of a policyholder, named as pvv-soat names it ("Holder"). */
export interface HolderDto {
  firstName: string;
  lastName: string;
  email: string;
  /** Local number without country code; the UI prepends "+54". */
  phone: string;
}
export interface LookupHolderRequest {
  documentType: DocumentType;
  documentNumber: string;
}
export interface LookupHolderResponse {
  found: boolean;
  /** The matched holder when `found`, otherwise null. */
  holder: HolderDto | null;
}

// ---- Quote (step 4, part 1) ------------------------------------------------
export interface GetQuoteRequest {
  plate: string;
  documentType: DocumentType;
  documentNumber: string;
}
export interface GetQuoteResponse {
  coverages: Coverage[];
}

// ---- Emission (step 5) -----------------------------------------------------
export interface EmitPolicyRequest {
  plate: string;
  coverageId: string;
  holder: HolderDto;
}
export interface EmitPolicyResponse {
  policyNumber: string;
  /** ISO date (yyyy-mm-ddTHH:mm:ss…) — the front formats it for display. */
  validFromIso: string;
  validUntilIso: string;
}

// ---- Budget (created at pay time) ------------------------------------------
export interface CreateBudgetRequest {
  plate: string;
  dni: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  productId: string;
  price: number;
}
export interface CreateBudgetResponse {
  budgetId: string;
  amount: number;
}

// ---- Payment init ----------------------------------------------------------
export interface StartPaymentRequest {
  budgetId: string;
  amount: number;
  /** Denormalized so the result page can show the ticket after the redirect. */
  vehicleTitle: string;
  holderName: string;
  /**
   * Ties the payment to the lead. The BFF stores it on the transaction and uses
   * it to record steps 4 and 5 on its own — this browser is about to be
   * redirected away and may never come back.
   */
  flowId: string;
}
export interface StartPaymentResponse {
  transactionId: string;
  /** URL to redirect to — Mercado Pago's checkout, or our mock one. */
  initPoint: string;
  amount: number;
}

// ---- Payment sync (once, on returning from the checkout) -------------------
export interface PaymentSyncResponse {
  transactionId: string;
  /** Pending | Confirmed | Failed | Abandoned. */
  paymentStatus: string;
  /** Whether the provider was actually consulted (false when there was nothing to do). */
  synced: boolean;
  /** What the confirmation decided: confirmed | failed | pending | already_confirmed. */
  outcome: string | null;
}

// ---- Emission status (polled on the result page) ---------------------------
export interface EmissionStatusResponse {
  transactionId: string;
  /** Pending | Confirmed | Failed | Abandoned. */
  paymentStatus: string;
  /**
   * Deadline (ISO) of a payment the provider has but the buyer has not completed —
   * a cash coupon or a transfer. Present only in that case, and it is what tells
   * "issuing your policy" apart from "you still have to go and pay".
   */
  paymentPendingUntil: string | null;
  /** pending | emitting | success | failed | retry-exhausted. */
  emissionStatus: string;
  policyNumber: string | null;
  amount: number;
  vehicleTitle: string | null;
  holderName: string | null;
  /** Real coverage window (ISO) — may be future-dated for a renewal. */
  validFrom: string | null;
  validUntil: string | null;
  emissionUpdatedAt: string | null;
}

// ---- Lead tracking ---------------------------------------------------------
/** Flat, primitive-only event data (the BFF reads it key by key). */
export type LeadEventPayload = Record<string, string | number | boolean | undefined>;

// ---- Holder ↔ Policyholder mapping ----------------------------------------
export function holderToPolicyholder(holder: HolderDto): Policyholder {
  return {
    firstName: holder.firstName,
    lastName: holder.lastName,
    email: holder.email,
    phone: holder.phone,
  };
}

export function policyholderToHolder(policyholder: Policyholder): HolderDto {
  return {
    firstName: policyholder.firstName,
    lastName: policyholder.lastName,
    email: policyholder.email,
    phone: policyholder.phone,
  };
}
