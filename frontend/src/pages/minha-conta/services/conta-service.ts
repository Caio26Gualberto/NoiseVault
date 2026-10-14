import { apiDelete, apiPatch, apiPostForm, apiPut } from '@/api/base-service'
import type { ProfileDto } from '@/api/types'
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
