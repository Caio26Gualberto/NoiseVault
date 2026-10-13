export type JwtPayload = {
  role: string | null
  email: string | null
  accountId: string | null
  exp: number | null
}

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
const EMAIL_CLAIM = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'

function decodeBase64Url(segment: string): string {
  const padded = segment.replace(/-/g, '+').replace(/_/g, '/')
  const binary = atob(padded.padEnd(padded.length + ((4 - (padded.length % 4)) % 4), '='))
  const bytes = Uint8Array.from(binary, (char) => char.charCodeAt(0))
  return new TextDecoder().decode(bytes)
}

function firstString(value: unknown): string | null {
  if (typeof value === 'string') return value
  if (Array.isArray(value) && typeof value[0] === 'string') return value[0]
  return null
}

export function decodeJwt(token: string): JwtPayload | null {
  const segments = token.split('.')
  if (segments.length !== 3) return null

  try {
    const claims = JSON.parse(decodeBase64Url(segments[1])) as Record<string, unknown>

    return {
      role: firstString(claims.role) ?? firstString(claims[ROLE_CLAIM]),
      email: firstString(claims[EMAIL_CLAIM]),
      accountId: firstString(claims.account_id),
      exp: typeof claims.exp === 'number' ? claims.exp : null,
    }
  } catch {
    return null
  }
}
