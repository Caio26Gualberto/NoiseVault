import type { AxiosInstance, AxiosResponse, InternalAxiosRequestConfig } from 'axios'

import { decodeJwt } from './jwt'
import { tokenStorage } from './token-storage'
import type { ApiEnvelope, TokensDto } from './types'

export const SESSION_EXPIRED_FLAG = 'appSessionExpired'

type RetriableConfig = InternalAxiosRequestConfig & {
  appRetried?: boolean
  [SESSION_EXPIRED_FLAG]?: boolean
}

const REFRESH_PATH = '/api/auth/refresh-token'
const REFRESH_MARGIN_MS = 60_000
const RACE_RETRY_MS = 400
const REFRESH_LOCK = 'nv-refresh'

let pendingRefresh: Promise<string | null> | null = null

const sleep = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms))

async function postRefresh(bareClient: AxiosInstance): Promise<ApiEnvelope<TokensDto> | null> {
  try {
    const response = await bareClient.post<ApiEnvelope<TokensDto>>(REFRESH_PATH)
    return response.data ?? null
  } catch {
    return null
  }
}

async function requestNewToken(bareClient: AxiosInstance): Promise<string | null> {
  let envelope = await postRefresh(bareClient)

  if (envelope && !envelope.isSuccess && envelope.code === 'INVALID_TOKEN') {
    await sleep(RACE_RETRY_MS)
    envelope = await postRefresh(bareClient)
  }

  if (!envelope?.isSuccess || !envelope.data?.token) return null

  tokenStorage.saveToken(envelope.data.token)
  return envelope.data.token
}

function requestAcrossTabs(bareClient: AxiosInstance): Promise<string | null> {
  if (typeof navigator === 'undefined' || !navigator.locks) return requestNewToken(bareClient)
  return navigator.locks.request(REFRESH_LOCK, () => requestNewToken(bareClient))
}

function refreshOnce(bareClient: AxiosInstance): Promise<string | null> {
  pendingRefresh ??= requestAcrossTabs(bareClient).finally(() => {
    pendingRefresh = null
  })
  return pendingRefresh
}

export function restoreSession(bareClient: AxiosInstance): Promise<string | null> {
  return refreshOnce(bareClient)
}

export async function freshToken(bareClient: AxiosInstance): Promise<string | null> {
  const token = tokenStorage.readToken()
  if (!token) return refreshOnce(bareClient)

  const exp = decodeJwt(token)?.exp
  if (exp != null && exp * 1000 - Date.now() > REFRESH_MARGIN_MS) return token

  return refreshOnce(bareClient)
}

export function attachAuthInterceptor(
  client: AxiosInstance,
  bareClient: AxiosInstance,
  onSessionExpired: () => void,
) {
  client.interceptors.response.use(async (response: AxiosResponse) => {
    const config = response.config as RetriableConfig

    const shouldRefresh =
      response.status === 401 &&
      !config.appRetried &&
      !config.url?.includes(REFRESH_PATH) &&
      tokenStorage.readToken() !== null

    if (!shouldRefresh) return response

    const newToken = await refreshOnce(bareClient)

    if (!newToken) {
      tokenStorage.clear()
      onSessionExpired()
      config[SESSION_EXPIRED_FLAG] = true
      response.config = config
      return response
    }

    const retryConfig: RetriableConfig = {
      ...config,
      appRetried: true,
      headers: { ...config.headers, Authorization: `Bearer ${newToken}` } as RetriableConfig['headers'],
    }

    return bareClient.request(retryConfig)
  })
}
