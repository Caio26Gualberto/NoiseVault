import { apiDelete, apiGet, apiPost, apiPut } from '@/api/base-service'
import type { PagedRequest, PagedResult } from '@/api/types'
import type { BandSummaryDto } from '@/pages/bandas/services/bandas-service'
import type { BandOpeningSummaryDto } from '@/pages/ensaio/services/ensaio-service'
import type { EventSummaryDto } from '@/pages/eventos/services/eventos-service'
import type { ArchiveSummaryDto } from '@/pages/mural/services/mural-service'
import type { ProfileSummaryDto } from '@/pages/perfil/services/perfil-service'

import type { PostStatus } from '../post-status'
import type { MediaDto } from './media-service'
import type { MentionDto } from '../mention'

export type PostDto = {
  id: number
  author: ProfileSummaryDto
  band: BandSummaryDto | null
  event: EventSummaryDto | null
  archive: ArchiveSummaryDto | null
  opening: BandOpeningSummaryDto | null
  content: string
  mentions: MentionDto[]
  media: MediaDto[]
  likeCount: number
  commentCount: number
  likedByMe: boolean
  likePreview: ProfileSummaryDto[]
  status: PostStatus
  createdAt: string
  editedAt: string | null
  canEdit: boolean
  canDelete: boolean
}

export type CommentDto = {
  id: number
  author: ProfileSummaryDto
  text: string
  mentions: MentionDto[]
  createdAt: string
  canDelete: boolean
}

export type CreatePostInput = {
  content: string
  bandSlug: string | null
  mediaIds: number[]
}

export function getFeed(query: PagedRequest): Promise<PagedResult<PostDto>> {
  return apiGet<PagedResult<PostDto>>('/api/posts', query)
}

export function getFollowingFeed(query: PagedRequest): Promise<PagedResult<PostDto>> {
  return apiGet<PagedResult<PostDto>>('/api/posts', { ...query, following: true })
}

export function getUserPosts(nickname: string, query: PagedRequest): Promise<PagedResult<PostDto>> {
  return apiGet<PagedResult<PostDto>>(`/api/users/${encodeURIComponent(nickname)}/posts`, query)
}

export function getBandPosts(slug: string, query: PagedRequest): Promise<PagedResult<PostDto>> {
  return apiGet<PagedResult<PostDto>>(`/api/bands/${encodeURIComponent(slug)}/posts`, query)
}

export function getPost(id: number): Promise<PostDto> {
  return apiGet<PostDto>(`/api/posts/${id}`, undefined, { toastError: false })
}

export function createPost(input: CreatePostInput): Promise<PostDto> {
  return apiPost<PostDto>('/api/posts', input)
}

export function updatePost(id: number, content: string): Promise<PostDto> {
  return apiPut<PostDto>(`/api/posts/${id}`, { content }, { toastSuccess: true })
}

export function deletePost(id: number): Promise<void> {
  return apiDelete<void>(`/api/posts/${id}`, { toastSuccess: true })
}

export function likePost(id: number): Promise<PostDto> {
  return apiPut<PostDto>(`/api/posts/${id}/like`)
}

export function unlikePost(id: number): Promise<PostDto> {
  return apiDelete<PostDto>(`/api/posts/${id}/like`)
}

export function getComments(id: number, query: PagedRequest): Promise<PagedResult<CommentDto>> {
  return apiGet<PagedResult<CommentDto>>(`/api/posts/${id}/comments`, query)
}

export function addComment(id: number, text: string): Promise<CommentDto> {
  return apiPost<CommentDto>(`/api/posts/${id}/comments`, { text })
}

export function deleteComment(id: number): Promise<void> {
  return apiDelete<void>(`/api/comments/${id}`, { toastSuccess: true })
}

export function getPostLikes(id: number, query: PagedRequest): Promise<PagedResult<ProfileSummaryDto>> {
  return apiGet<PagedResult<ProfileSummaryDto>>(`/api/posts/${id}/likes`, query)
}
