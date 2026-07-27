/** Where pvv-front is served; the portal link is built against it. */
export const PORTAL_BASE_URL = import.meta.env.VITE_PORTAL_BASE_URL ?? 'http://localhost:5173'

/**
 * The public URL of a company's point of sale. The hash in `?c=` is what the
 * portal resolves the tenant from — it is the link handed to a new company.
 */
export function portalUrl(companyToken: string | null): string | null {
  return companyToken ? `${PORTAL_BASE_URL}/?c=${encodeURIComponent(companyToken)}` : null
}
