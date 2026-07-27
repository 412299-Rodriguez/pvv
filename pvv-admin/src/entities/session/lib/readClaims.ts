/**
 * Reads a claim out of the operator's JWT.
 *
 * The token is already in the browser and signed by pvv-config — this only
 * decodes it for display purposes. Nothing here is a security check: every
 * authorization decision is made server-side against the signature.
 */
function decodePayload(jwt: string): Record<string, unknown> | null {
  try {
    const payload = jwt.split('.')[1]
    if (!payload) return null

    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/')
    const binary = atob(base64)
    const bytes = Uint8Array.from(binary, (char) => char.charCodeAt(0))
    return JSON.parse(new TextDecoder().decode(bytes)) as Record<string, unknown>
  } catch {
    return null
  }
}

/**
 * The company's portal hash, present only for a company operator. Used to build
 * the portal link that goes inside a recovery email.
 */
export function readCompanyToken(jwt: string | null): string | null {
  if (!jwt) return null
  const claims = decodePayload(jwt)
  const token = claims?.companyToken
  return typeof token === 'string' && token.length > 0 ? token : null
}
