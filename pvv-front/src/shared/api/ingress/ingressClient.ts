/**
 * Ingress client — the front's only gateway to pvv-bff.
 *
 * MOCK (HU-05): every function returns fixture data after a simulated latency,
 * so the whole wizard runs with no backend. The function SIGNATURES are final.
 *
 * HU-10: replace each body with a real call through `axiosInstance` (which
 * already attaches X-Company-Token / X-Session-Id), e.g.
 *
 *     export async function checkPlate(req: CheckPlateRequest) {
 *       const { data } = await axiosInstance.post<CheckPlateResponse>(
 *         '/ingress/plate-check', req);
 *       return data;
 *     }
 *
 * Because callers only depend on the signatures, that swap is a drop-in.
 */
import { generatePolicyNumber } from '@/entities/policy';

import { axiosInstance } from '../axiosInstance';
import { ingressRequest, IngressError } from './ingressRequest';
import type {
  CheckPlateRequest,
  CheckPlateResponse,
  LookupHolderRequest,
  LookupHolderResponse,
  GetQuoteRequest,
  GetQuoteResponse,
  EmitPolicyRequest,
  EmitPolicyResponse,
  CreateBudgetRequest,
  CreateBudgetResponse,
  StartPaymentRequest,
  StartPaymentResponse,
  EmissionStatusResponse,
  LeadEventPayload,
} from './contracts';

/** Emission is intentionally slower so the "Emitiendo…" screen is visible. */
const MOCK_EMIT_LATENCY_MS = 2200;

const delay = (ms: number) => new Promise<void>((resolve) => window.setTimeout(resolve, ms));

export async function checkPlate(req: CheckPlateRequest): Promise<CheckPlateResponse> {
  // Real (HU-10): PLATE_SEARCH aggregates the vehicle + active policy in the BFF.
  // A 404 (unknown plate) surfaces as an IngressError the caller handles.
  return ingressRequest<CheckPlateResponse>('PLATE_SEARCH', { plate: req.plate });
}

/** soat's holder shape (subset) returned by HOLDER_LOOKUP. */
interface SoatHolderResponse {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
}

export async function lookupHolder(req: LookupHolderRequest): Promise<LookupHolderResponse> {
  // Real (HU-10): HOLDER_LOOKUP proxies GET /api/holders/{dni} in soat.
  try {
    const holder = await ingressRequest<SoatHolderResponse>('HOLDER_LOOKUP', {
      dni: req.documentNumber,
    });
    return {
      found: true,
      holder: {
        firstName: holder.firstName,
        lastName: holder.lastName,
        email: holder.email,
        phone: holder.phone,
      },
    };
  } catch (error) {
    // 404 → no customer with that document; the user fills the form manually.
    if (error instanceof IngressError && error.statusCode === 404) {
      return { found: false, holder: null };
    }
    throw error;
  }
}

export async function getQuote(req: GetQuoteRequest): Promise<GetQuoteResponse> {
  // Real (HU-10): QUOTE builds the coverages from the company's PRODUCT_CONFIG +
  // PRICING_CONFIG, priced by the vehicle's type and year.
  return ingressRequest<GetQuoteResponse>('QUOTE', { plate: req.plate });
}

export async function createBudget(req: CreateBudgetRequest): Promise<CreateBudgetResponse> {
  // BUDGET_CALC re-resolves the soat ids in the BFF and creates the budget.
  return ingressRequest<CreateBudgetResponse>('BUDGET_CALC', { ...req });
}

export async function startPayment(req: StartPaymentRequest): Promise<StartPaymentResponse> {
  // PAYMENT_INIT creates the pending transaction + checkout preference.
  return ingressRequest<StartPaymentResponse>('PAYMENT_INIT', { ...req });
}

export async function getEmissionStatus(transactionId: string): Promise<EmissionStatusResponse> {
  // EMISSION_STATUS — polled by the result page after the redirect.
  return ingressRequest<EmissionStatusResponse>('EMISSION_STATUS', { transactionId });
}

/**
 * Mock-checkout → webhook. Stands in for Mercado Pago calling our server-to-server
 * webhook; this is NOT an ingress call (the webhook is anonymous, outside /api/ingress).
 */
export async function confirmMockPayment(transactionId: string, approved: boolean): Promise<void> {
  await axiosInstance.post('/api/payments/webhook', {
    transactionId,
    status: approved ? 'approved' : 'rejected',
  });
}

/**
 * Reports one wizard event to the BFF, which projects it onto the lead.
 *
 * Deliberately fire-and-forget: tracking must never delay the wizard or surface
 * an error to the buyer, so the promise is swallowed. Only the events the client
 * is allowed to raise are accepted — the payment and emission ones are recorded
 * by the BFF itself.
 */
export function sendLeadEvent(event: string, flowId: string, payload?: LeadEventPayload): void {
  void ingressRequest('LEAD_EVENT', { event, flowId, payload: payload ?? {} }).catch(() => {
    // Ignored on purpose: analytics must not break the purchase.
  });
}

export async function emitPolicy(_req: EmitPolicyRequest): Promise<EmitPolicyResponse> {
  await delay(MOCK_EMIT_LATENCY_MS);
  const from = new Date();
  const until = new Date();
  until.setFullYear(until.getFullYear() + 1);
  return {
    policyNumber: generatePolicyNumber(),
    validFromIso: from.toISOString(),
    validUntilIso: until.toISOString(),
  };
}
