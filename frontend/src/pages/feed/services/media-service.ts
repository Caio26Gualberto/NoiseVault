import { apiDelete, apiGet, apiPost, apiPostForm } from '@/api/base-service'
import { uploadParts } from '@/api/multipart-upload'
import type { UploadProgress } from '@/api/multipart-upload'
import { videoContentType } from '@/lib/video-file'
import type { ProfileSummaryDto } from '@/pages/perfil/services/perfil-service'

import type { MediaKind } from '../media-kind'
import type { MediaStatus } from '../media-status'

export type MediaDto = {
  id: number
  kind: MediaKind
  status: MediaStatus
  url: string | null
  posterUrl: string | null
  width: number | null
  height: number | null
  durationSeconds: number | null
  tags: PhotoTagDto[]
}

export type PhotoTagDto = {
  id: number
  x: number
  y: number
  user: ProfileSummaryDto | null
  name: string | null
  canRemove: boolean
}

export type PhotoTagInput = { x: number; y: number; nickname?: string; name?: string }

export type VideoUploadDto = {
  id: number
  partSize: number
  parts: { partNumber: number; url: string }[]
  expiresAt: string
}

export function uploadPhoto(file: File): Promise<MediaDto> {
  const form = new FormData()
  form.append('file', file)
  return apiPostForm<MediaDto>('/api/media/photos', form)
}

export function getMedia(id: number): Promise<MediaDto> {
  return apiGet<MediaDto>(`/api/media/${id}`, undefined, { toastError: false })
}

export function deleteMedia(id: number): Promise<void> {
  return apiDelete<void>(`/api/media/${id}`, { toastError: false })
}

export async function uploadVideo(
  file: File,
  onProgress: UploadProgress,
  signal: AbortSignal,
  startPath = '/api/media/videos',
): Promise<MediaDto> {
  const upload = await apiPost<VideoUploadDto>(startPath, {
    sizeBytes: file.size,
    contentType: videoContentType(file),
  })

  try {
    const parts = await uploadParts(file, upload.parts, { partSize: upload.partSize, onProgress, signal })
    return await apiPost<MediaDto>(`/api/media/videos/${upload.id}/complete`, { parts })
  } catch (error) {
    void deleteMedia(upload.id).catch(() => undefined)
    throw error
  }
}

export function addPhotoTag(mediaId: number, input: PhotoTagInput): Promise<PhotoTagDto[]> {
  return apiPost<PhotoTagDto[]>(`/api/media/${mediaId}/tags`, input, { toastSuccess: true })
}

export function removePhotoTag(id: number): Promise<void> {
  return apiDelete<void>(`/api/photo-tags/${id}`, { toastSuccess: true })
}
