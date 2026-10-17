export const ExternalProvider = {
  google: 1,
  facebook: 2,
} as const

export type ExternalProvider = (typeof ExternalProvider)[keyof typeof ExternalProvider]

export const providerNames: Record<ExternalProvider, string> = {
  [ExternalProvider.google]: 'Google',
  [ExternalProvider.facebook]: 'Facebook',
}

export const googleClientId = (import.meta.env.VITE_GOOGLE_CLIENT_ID ?? '').trim()
export const facebookAppId = (import.meta.env.VITE_FACEBOOK_APP_ID ?? '').trim()

export const isGoogleEnabled = googleClientId.length > 0
export const isFacebookEnabled = facebookAppId.length > 0
export const isAnyProviderEnabled = isGoogleEnabled || isFacebookEnabled

export function isProviderEnabled(provider: ExternalProvider) {
  return provider === ExternalProvider.google ? isGoogleEnabled : isFacebookEnabled
}

export function isInAppBrowser() {
  return typeof navigator !== 'undefined' && /FBAN|FBAV|FB_IAB|Instagram/i.test(navigator.userAgent)
}
