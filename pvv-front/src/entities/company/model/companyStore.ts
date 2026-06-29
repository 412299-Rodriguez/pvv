import { create } from 'zustand';

/**
 * Resolved insurer company — RESERVED for HU-10 (integration / multi-tenancy).
 *
 * The portal URL carries `?c=<HashedCompanyId>` (the AES-256 token from
 * pvv-config). HU-10 reads that token at app start, resolves the company through
 * the BFF, and populates this store; the ingress client then sends the token as
 * `X-Company-Token`. Not wired yet.
 */
export interface CompanyIdentity {
  /** The `?c=` token (HashedCompanyId); null until resolved. */
  token: string | null;
  name: string | null;
}

interface CompanyState extends CompanyIdentity {
  setCompany: (identity: CompanyIdentity) => void;
}

export const useCompanyStore = create<CompanyState>((set) => ({
  token: null,
  name: null,
  setCompany: (identity) => set(identity),
}));
