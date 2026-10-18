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
    fbAsyncInit?: () => void
  }
}

const sdkUrl = 'https://connect.facebook.net/pt_BR/sdk.js'
const graphVersion = 'v24.0'

let ready: Promise<FacebookSdk> | null = null

export function loadFacebook(): Promise<FacebookSdk> {
  ready ??= new Promise<FacebookSdk>((resolve, reject) => {
    window.fbAsyncInit = () => {
      const sdk = window.FB
      if (!sdk) {
        reject(new Error('facebook'))
        return
      }
      sdk.init({ appId: facebookAppId, version: graphVersion, cookie: false, xfbml: false, status: false })
      resolve(sdk)
    }
    loadScript(sdkUrl).catch(reject)
  }).catch((error: unknown) => {
    ready = null
    throw error
  })
  return ready
}

export function facebookLogin(sdk: FacebookSdk): Promise<string | null> {
  const current = window.FB ?? sdk
  return new Promise((resolve) => {
    current.login(
      (response) => resolve(response.status === 'connected' ? (response.authResponse?.accessToken ?? null) : null),
      { scope: 'public_profile,email' },
    )
  })
}

export type { FacebookSdk }
