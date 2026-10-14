import axios from 'axios'
import type { AxiosRequestConfig } from 'axios'

import { attachAuthInterceptor, freshToken } from './auth-interceptor'
import { tokenStorage } from './token-storage'

function resolveApiBaseUrl(): string {
  const value = import.meta.env.VITE_API_BASE_URL

  if (!value) {
    throw new Error(
      'VITE_API_BASE_URL não definida. Configure a variável no .env local (veja .env.example).',
    )
  }

  return value.replace(/\/+$/, '')
}

export const apiBaseUrl = resolveApiBaseUrl()

const baseConfig: AxiosRequestConfig = {
  baseURL: apiBaseUrl,
  timeout: 15000,
  headers: { 'Content-Type': 'application/json' },
  validateStatus: (status) => status < 500,
}

export const bareClient = axios.create(baseConfig)

export const apiClient = axios.create(baseConfig)

apiClient.interceptors.request.use((config) => {
  const token = tokenStorage.readToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

let sessionExpiredHandler: () => void = () => {}

export function setSessionExpiredHandler(handler: () => void) {
  sessionExpiredHandler = handler
}

attachAuthInterceptor(apiClient, bareClient, () => sessionExpiredHandler())

export function getFreshToken(): Promise<string | null> {
  return freshToken(bareClient)
}
