import { loadScript } from './load-script'
import { facebookAppId } from './providers'

type FacebookLoginResponse = {
  status: 'connected' | 'not_authorized' | 'unknown'
  authResponse?: { accessToken: string } | null
}

type FacebookSdk = {
  init: (options: { appId: string; version: string; cookie?: boolean; xfbml?: boolean; status?: boolean }) => void
  login: (callback: (response: FacebookLoginResponse) => void, options?: { scope?: string; auth_type?: string }) => void
}

declare global {
  interface Window {
    FB?: FacebookSdk
  }
}

const sdkUrl = 'https://connect.facebook.net/pt_BR/sdk.js'
const graphVersion = 'v24.0'

let ready: Promise<FacebookSdk> | null = null

export function loadFacebook(): Promise<FacebookSdk> {
  ready ??= loadScript(sdkUrl)
    .then(() => {
      const sdk = window.FB
      if (!sdk) throw new Error('facebook')
      sdk.init({ appId: facebookAppId, version: graphVersion, cookie: false, xfbml: false, status: false })
      return sdk
    })
    .catch((error: unknown) => {
      ready = null
      throw error
    })
  return ready
}

export function facebookLogin(sdk: FacebookSdk): Promise<string | null> {
  return new Promise((resolve) => {
    sdk.login(
      (response) => resolve(response.status === 'connected' ? (response.authResponse?.accessToken ?? null) : null),
      { scope: 'public_profile,email' },
    )
  })
}

export type { FacebookSdk }
