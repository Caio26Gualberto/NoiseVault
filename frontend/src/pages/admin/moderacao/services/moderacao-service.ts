import type { PagedRequest, PagedResult } from '@/api/types'
import type { ProfileSummaryDto } from '@/pages/perfil/services/perfil-service'

import * as mocks from './mocks'

export type ModerationKind = 'photo' | 'video'

export type ModerationItemDto = {
  id: number
  kind: ModerationKind
  title: string
  subtitle: string | null
  author: ProfileSummaryDto
  submittedAt: string
  previewUrl: string
}

export type ModerationQuery = PagedRequest & { kind: ModerationKind | 'all' }

export const getQueue: (query: ModerationQuery) => Promise<PagedResult<ModerationItemDto>> = mocks.getQueue
export const approve = mocks.approve
export const reject = mocks.reject
