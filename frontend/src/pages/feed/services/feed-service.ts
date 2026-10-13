import type { PagedRequest, PagedResult } from '@/api/types'
import type { BandSummaryDto, ProfileSummaryDto } from '@/pages/perfil/services/perfil-service'

import * as mocks from './mocks'

export type PostMediaDto = {
  id: number
  kind: 'photo' | 'video'
  url: string
}

export type PostDto = {
  id: number
  author: ProfileSummaryDto
  band: BandSummaryDto | null
  content: string
  media: PostMediaDto[]
  likeCount: number
  commentCount: number
  likedByMe: boolean
  createdAt: string
}

export type CommentDto = {
  id: number
  author: ProfileSummaryDto
  text: string
  createdAt: string
}

export type FeedQuery = PagedRequest & { scope: 'all' | 'following' }

export const getFeed: (query: FeedQuery) => Promise<PagedResult<PostDto>> = mocks.getFeed
export const getPost = mocks.getPost
export const toggleLike = mocks.toggleLike
export const getComments = mocks.getComments
export const addComment = mocks.addComment
export const createPost = mocks.createPost
