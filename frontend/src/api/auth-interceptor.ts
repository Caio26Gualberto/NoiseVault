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

let pendingRefresh: Promise<string | null> | null = null

async function requestNewToken(bareClient: AxiosInstance): Promise<string | null> {
  const refreshToken = tokenStorage.readRefreshToken()
  if (!refreshToken) return null

  try {
    const response = await bareClient.post<ApiEnvelope<TokensDto>>(REFRESH_PATH, { refreshToken })
    const envelope = response.data

    if (!envelope?.isSuccess || !envelope.data?.token) return null

    tokenStorage.saveTokens(envelope.data.token, envelope.data.refreshToken)
    return envelope.data.token
  } catch {
    return null
  }
}

function refreshOnce(bareClient: AxiosInstance): Promise<string | null> {
  pendingRefresh ??= requestNewToken(bareClient).finally(() => {
    pendingRefresh = null
  })
  return pendingRefresh
}

export async function freshToken(bareClient: AxiosInstance): Promise<string | null> {
  const token = tokenStorage.readToken()
  if (!token) return null

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
      tokenStorage.readRefreshToken() !== null

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
