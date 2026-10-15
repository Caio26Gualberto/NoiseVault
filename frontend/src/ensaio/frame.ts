const ensaioPrefix = '/ensaio'
const configuredEnsaioUrl = (import.meta.env.VITE_ENSAIO_URL ?? '').replace(/\/+$/, '')
const configuredMainUrl = (import.meta.env.VITE_MAIN_URL ?? '').replace(/\/+$/, '')

export const isEnsaioHost = typeof window !== 'undefined' && window.location.hostname.startsWith('ensaio.')

export const ensaioBasePath = isEnsaioHost ? '' : ensaioPrefix

function normalize(path: string) {
  return path.startsWith('/') ? path : `/${path}`
}

export function ensaioPath(path = '/') {
  const target = normalize(path)
  if (isEnsaioHost) return target
  return target === '/' ? ensaioPrefix : `${ensaioPrefix}${target}`
}

export function ensaioUrl(path = '/') {
  if (isEnsaioHost) return normalize(path)
  if (configuredEnsaioUrl) return `${configuredEnsaioUrl}${normalize(path)}`
  return ensaioPath(path)
}

export function mainUrl(path = '/') {
  if (!isEnsaioHost) return normalize(path)
  if (configuredMainUrl) return `${configuredMainUrl}${normalize(path)}`
  const { protocol, host } = window.location
  return `${protocol}//${host.replace(/^ensaio\./, '')}${normalize(path)}`
}

export function isExternalUrl(url: string) {
  return /^https?:\/\//i.test(url)
}

function ensaioOrigin() {
  if (configuredEnsaioUrl) return new URL(configuredEnsaioUrl).origin
  const { protocol, host } = window.location
  return `${protocol}//ensaio.${host.replace(/^ensaio\./, '')}`
}

export function safeReturnUrl(value: string | null) {
  if (!value) return null
  try {
    const url = new URL(value)
    return url.origin === ensaioOrigin() ? url.toString() : null
  } catch {
    return null
  }
}

export const returnParam = 'voltar'

export function returnUrlFrom(search: string) {
  return safeReturnUrl(new URLSearchParams(search).get(returnParam))
}

export function loginUrlReturningHere() {
  return mainUrl(`/entrar?${returnParam}=${encodeURIComponent(window.location.href)}`)
}
