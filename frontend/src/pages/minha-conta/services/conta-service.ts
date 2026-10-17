import { apiDelete, apiGet, apiPatch, apiPost, apiPostForm, apiPut } from '@/api/base-service'
import type { ProfileDto, SignInMethodsDto } from '@/api/types'
import type { ExternalProvider } from '@/auth/external/providers'
import type { MusicGenre } from '@/pages/perfil/genres'
import type { SocialLinksDto } from '@/pages/perfil/services/perfil-service'

export type UpdateProfileInput = {
  name: string
  bio: string | null
  cityId: number | null
  cityName: string | null
  mainInstrument: string | null
  genres: MusicGenre[]
  socialLinks: SocialLinksDto
}

export function updateProfile(input: UpdateProfileInput) {
  return apiPut<ProfileDto>('/api/profile', input, { toastSuccess: true })
}

export function updateNotificationSettings(input: { emailOnBandInvite: boolean }) {
  return apiPut<ProfileDto>('/api/profile/notification-settings', input, { toastSuccess: true })
}

export function setApproxLocation(latitude: number, longitude: number) {
  return apiPut<ProfileDto>('/api/profile/location', { latitude, longitude }, { toastSuccess: true })
}

export function clearApproxLocation() {
  return apiDelete<ProfileDto>('/api/profile/location', { toastSuccess: true })
}

export function setAvatar(file: File) {
  const form = new FormData()
  form.append('file', file)
  return apiPostForm<ProfileDto>('/api/profile/avatar', form, { toastSuccess: true })
}

export function removeAvatar() {
  return apiDelete<ProfileDto>('/api/profile/avatar', { toastSuccess: true })
}

export function setBanner(file: File, position: number) {
  const form = new FormData()
  form.append('file', file)
  form.append('position', String(position))
  return apiPostForm<ProfileDto>('/api/profile/banner', form, { toastSuccess: true })
}

export function setBannerPosition(position: number) {
  return apiPatch<ProfileDto>('/api/profile/banner-position', { position }, { toastSuccess: true })
}

export function removeBanner() {
  return apiDelete<ProfileDto>('/api/profile/banner', { toastSuccess: true })
}

export function getSignInMethods() {
  return apiGet<SignInMethodsDto>('/api/profile/sign-in-methods', undefined, { toastError: false })
}

export function linkSignInMethod(provider: ExternalProvider, token: string) {
  return apiPost<SignInMethodsDto>('/api/profile/sign-in-methods', { provider, token }, { toastSuccess: true })
}

export function unlinkSignInMethod(provider: ExternalProvider) {
  return apiDelete<SignInMethodsDto>(`/api/profile/sign-in-methods/${provider}`, { toastSuccess: true })
}

export function createPassword(newPassword: string) {
  return apiPost<SignInMethodsDto>('/api/profile/create-password', { newPassword }, { toastSuccess: true })
}
