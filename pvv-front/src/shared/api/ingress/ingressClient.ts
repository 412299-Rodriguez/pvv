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
import { availableCoverages } from '@/entities/coverage';

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
} from './contracts';

/** Simulated network round-trip for lookups. */
const MOCK_LATENCY_MS = 600;
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

export async function getQuote(_req: GetQuoteRequest): Promise<GetQuoteResponse> {
  await delay(MOCK_LATENCY_MS);
  return { coverages: availableCoverages };
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
