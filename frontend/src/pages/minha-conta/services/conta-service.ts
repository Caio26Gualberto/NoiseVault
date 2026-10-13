import { apiPatch } from '@/api/base-service'
import type { ProfileDto } from '@/api/types'

export function updateName(name: string) {
  return apiPatch<ProfileDto>('/api/profile/name', { name }, { toastSuccess: true })
}
