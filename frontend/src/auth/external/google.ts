import { loadScript } from './load-script'
import { googleClientId } from './providers'

type GoogleTokenResponse = { access_token?: string; error?: string }

type GoogleTokenClient = {
  requestAccessToken: (overrides?: { prompt?: '' | 'none' | 'consent' | 'select_account' }) => void
}

type GoogleOAuth2 = {
  initTokenClient: (options: {
    client_id: string
    scope: string
    callback: (response: GoogleTokenResponse) => void
    error_callback?: (error: { type?: string }) => void
  }) => GoogleTokenClient
}

declare global {
  interface Window {
    google?: { accounts: { oauth2: GoogleOAuth2 } }
  }
}

const sdkUrl = 'https://accounts.google.com/gsi/client'
const scope = 'openid email profile'

let pending: ((token: string | null) => void) | null = null
let ready: Promise<GoogleTokenClient> | null = null

function settle(token: string | null) {
  pending?.(token)
  pending = null
}

export function loadGoogle(): Promise<GoogleTokenClient> {
  ready ??= loadScript(sdkUrl)
    .then(() => {
      const oauth2 = window.google?.accounts.oauth2
      if (!oauth2) throw new Error('google')
      return oauth2.initTokenClient({
        client_id: googleClientId,
        scope,
        callback: (response) => settle(response.access_token ?? null),
        error_callback: () => settle(null),
      })
    })
    .catch((error: unknown) => {
      ready = null
      throw error
    })
  return ready
}

export function googleLogin(client: GoogleTokenClient): Promise<string | null> {
  settle(null)
  return new Promise((resolve) => {
    pending = resolve
    client.requestAccessToken({ prompt: 'select_account' })
  })
}

export type { GoogleTokenClient }
