import { apiGet, apiPost } from '../api/base-service'
import type {
  ConfirmEmailResultDto,
  LoginResponseDto,
  ProfileDto,
  RegistrationPendingDto,
  TokensDto,
} from '../api/types'

export type RoleRef = {
  id: number
  name: string
}

export type MyAccess = {
  roles: RoleRef[]
  rolePermissions: string[]
  userPermissions: string[]
  effectivePermissions: string[]
}

export const emptyAccess: MyAccess = {
  roles: [],
  rolePermissions: [],
  userPermissions: [],
  effectivePermissions: [],
}

export type RegisterInput = {
  name: string
  nickname: string
  email: string
  password: string
}

export function login(email: string, password: string) {
  return apiPost<LoginResponseDto>('/api/auth/login', { email, password })
}

export function logout() {
  return apiPost<boolean>('/api/auth/logout', undefined, { toastError: false })
}

export function fetchMe() {
  return apiGet<ProfileDto>('/api/profile', undefined, { toastError: false })
}

export function fetchMyAccess() {
  return apiGet<MyAccess>('/api/admin/authorization/me', undefined, { toastError: false })
}

export function register(input: RegisterInput) {
  return apiPost<RegistrationPendingDto>('/api/auth/register', input)
}

export function confirmEmail(userId: number, token: string) {
  return apiPost<ConfirmEmailResultDto>('/api/auth/confirm-email', { userId, token }, { toastError: false })
}

export function resendConfirmation(email: string) {
  return apiPost<null>('/api/auth/resend-confirmation', { email }, { toastSuccess: true })
}

export function forgotPassword(email: string) {
  return apiPost<null>('/api/auth/forgot-password', { email })
}

export function resetPassword(email: string, token: string, newPassword: string) {
  return apiPost<null>('/api/auth/reset-password', { email, token, newPassword }, { toastSuccess: true })
}

export function changePassword(currentPassword: string, newPassword: string) {
  return apiPost<TokensDto>('/api/profile/change-password', { currentPassword, newPassword }, { toastSuccess: true })
}

export function setInitialPassword(newPassword: string) {
  return apiPost<TokensDto>('/api/profile/set-initial-password', { newPassword }, { toastSuccess: true })
}
