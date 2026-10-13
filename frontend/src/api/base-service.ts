import type { AxiosResponse } from 'axios'

import { ApiCodes } from './api-codes'
import { SESSION_EXPIRED_FLAG } from './auth-interceptor'
import { apiClient } from './client'
import { feedback } from './feedback-sink'
import { ApiError } from './types'
import type { ApiEnvelope } from './types'

export type RequestOptions = {
  toastSuccess?: boolean
  toastError?: boolean
}

async function request<T>(
  send: () => Promise<AxiosResponse<ApiEnvelope<T>>>,
  { toastSuccess = false, toastError = true }: RequestOptions,
): Promise<T> {
  let response: AxiosResponse<ApiEnvelope<T>>

  try {
    response = await send()
  } catch (error) {
    const code = isTimeoutOrNetwork(error) ? ApiCodes.networkError : ApiCodes.unknownError
    if (toastError) feedback.error(code, '')
    throw new ApiError(code)
  }

  const envelope = response.data

  if (!envelope || typeof envelope.isSuccess !== 'boolean') {
    if (toastError) feedback.error(ApiCodes.unknownError, '')
    throw new ApiError(ApiCodes.unknownError)
  }

  const httpOk = response.status >= 200 && response.status < 300

  if (!envelope.isSuccess || !httpOk) {
    const alreadyAnnounced = Boolean(
      (response.config as unknown as Record<string, unknown>)?.[SESSION_EXPIRED_FLAG],
    )
    if (toastError && !alreadyAnnounced) {
      feedback.error(envelope.code, envelope.message)
    }
    throw new ApiError(envelope.code, envelope.message)
  }

  if (toastSuccess) {
    feedback.success(envelope.code, envelope.message)
  }

  return envelope.data as T
}

function isTimeoutOrNetwork(error: unknown): boolean {
  const code = (error as { code?: string })?.code
  return code === 'ECONNABORTED' || code === 'ETIMEDOUT' || code === 'ERR_NETWORK'
}

export function apiGet<T>(
  path: string,
  params?: Record<string, unknown>,
  options: RequestOptions = {},
): Promise<T> {
  return request<T>(() => apiClient.get<ApiEnvelope<T>>(path, { params }), options)
}

export function apiPost<T>(path: string, body?: unknown, options: RequestOptions = {}): Promise<T> {
  return request<T>(() => apiClient.post<ApiEnvelope<T>>(path, body), options)
}

export function apiPostForm<T>(path: string, formData: FormData, options: RequestOptions = {}): Promise<T> {
  return request<T>(
    () => apiClient.post<ApiEnvelope<T>>(path, formData, { headers: { 'Content-Type': undefined } }),
    options,
  )
}

export function apiPatch<T>(path: string, body?: unknown, options: RequestOptions = {}): Promise<T> {
  return request<T>(() => apiClient.patch<ApiEnvelope<T>>(path, body), options)
}

export function apiPut<T>(path: string, body?: unknown, options: RequestOptions = {}): Promise<T> {
  return request<T>(() => apiClient.put<ApiEnvelope<T>>(path, body), options)
}

export function apiDelete<T>(path: string, options: RequestOptions = {}): Promise<T> {
  return request<T>(() => apiClient.delete<ApiEnvelope<T>>(path), options)
}
