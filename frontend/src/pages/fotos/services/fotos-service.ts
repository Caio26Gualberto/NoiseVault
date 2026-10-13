import type { PagedRequest, PagedResult } from '@/api/types'
import type { ProfileSummaryDto } from '@/pages/perfil/services/perfil-service'
import type { ModerationStatus } from '@/pages/videos/moderation-status'

import * as mocks from './mocks'

export type PhotoDto = {
  id: number
  title: string
  subtitle: string | null
  description: string | null
  imageUrl: string
  captureDate: string
  author: ProfileSummaryDto
  status: ModerationStatus
  eventId: number | null
}

export const getPhotos: (query: PagedRequest) => Promise<PagedResult<PhotoDto>> = mocks.getPhotos
