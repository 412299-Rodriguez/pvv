/** Argentine identity document types accepted by the portal. */
export const DOCUMENT_TYPES = ['DNI', 'CUIL', 'CUIT'] as const;
export type DocumentType = (typeof DOCUMENT_TYPES)[number];

/** The person who will hold the insurance policy. */
export interface Policyholder {
  firstName: string;
  lastName: string;
  email: string;
  /** Local number without country code; the UI prepends "+54". */
  phone: string;
}

/** Empty policyholder used as the initial form state. */
export const emptyPolicyholder: Policyholder = {
  firstName: '',
  lastName: '',
  email: '',
  phone: '',
};

/** "Juan Pérez" */
export function policyholderFullName(p: Policyholder): string {
  return `${p.firstName} ${p.lastName}`.trim();
}
