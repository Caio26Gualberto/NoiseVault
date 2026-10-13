import type { PagedRequest, PagedResult } from '@/api/types'
import type { ProfileSummaryDto } from '@/pages/perfil/services/perfil-service'

import type { ModerationStatus } from '../moderation-status'
import * as mocks from './mocks'

export type VideoDto = {
  id: number
  title: string
  description: string
  youtubeId: string
  author: ProfileSummaryDto
  status: ModerationStatus
  createdAt: string
}

export type VideoListQuery = PagedRequest & { search: string }

export const getVideos: (query: VideoListQuery) => Promise<PagedResult<VideoDto>> = mocks.getVideos
export const submitVideo = mocks.submitVideo
