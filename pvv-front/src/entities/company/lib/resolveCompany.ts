import { requestContext } from '@/shared/api';

import { useCompanyStore } from '../model/companyStore';

/**
 * Reads the `?c=<token>` URL param (the company's HashedCompanyId), stores it in
 * the company store, and wires it into the HTTP context so every ingress call
 * carries `X-Company-Token`. Returns the token (or null when absent).
 */
export function resolveCompanyFromUrl(): string | null {
  const token = new URLSearchParams(window.location.search).get('c');
  if (!token) {
    return null;
  }
  useCompanyStore.getState().setCompany({ token, name: null });
  requestContext.setCompanyToken(token);
  return token;
}
