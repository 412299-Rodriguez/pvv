/**
 * Mutable request context the axios interceptor reads on every call. The app
 * sets these as it resolves them (company token from `?c=`, the Turnstile token).
 * Kept in `shared` so the HTTP client never reaches up into entities/features.
 */

export const SESSION_ID_KEY = 'pvv-session-id';
const CLIENT_ID_KEY = 'pvv-client-id';
const COMPANY_TOKEN_KEY = 'pvv-company-token';

// Persisted so it survives the payment redirect (mock-checkout → result page),
// where the `?c=` token is no longer in the URL.
let companyToken: string | null = localStorage.getItem(COMPANY_TOKEN_KEY);
let turnstileToken: string | null = null;

export const requestContext = {
  get companyToken(): string | null {
    return companyToken;
  },
  setCompanyToken(value: string | null): void {
    companyToken = value;
    if (value) localStorage.setItem(COMPANY_TOKEN_KEY, value);
    else localStorage.removeItem(COMPANY_TOKEN_KEY);
  },
  get turnstileToken(): string | null {
    return turnstileToken;
  },
  setTurnstileToken(value: string | null): void {
    turnstileToken = value;
  },
};

/** A stable per-browser id used for fingerprinting; created on first use. */
export function getClientId(): string {
  let id = localStorage.getItem(CLIENT_ID_KEY);
  if (!id) {
    id = crypto.randomUUID();
    localStorage.setItem(CLIENT_ID_KEY, id);
  }
  return id;
}
