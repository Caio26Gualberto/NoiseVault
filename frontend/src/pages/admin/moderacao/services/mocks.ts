import { delay, mockPage } from '@/api/mock-delay'
import type { PagedResult } from '@/api/types'
import { photos } from '@/pages/fotos/services/mocks'
import { ModerationStatus } from '@/pages/videos/moderation-status'
import { videos } from '@/pages/videos/services/mocks'

import type { ModerationItemDto, ModerationQuery } from './moderacao-service'

function queue(): ModerationItemDto[] {
  const pendingVideos: ModerationItemDto[] = videos
    .filter((video) => video.status === ModerationStatus.pending)
    .map((video) => ({
      id: video.id,
      kind: 'video',
      title: video.title,
      subtitle: video.description,
      author: video.author,
      submittedAt: video.createdAt,
      previewUrl: `https://i.ytimg.com/vi/${video.youtubeId}/hqdefault.jpg`,
    }))

  const pendingPhotos: ModerationItemDto[] = photos
    .filter((photo) => photo.status === ModerationStatus.pending)
    .map((photo) => ({
      id: photo.id,
      kind: 'photo',
      title: photo.title,
      subtitle: photo.subtitle,
      author: photo.author,
      submittedAt: photo.captureDate,
      previewUrl: photo.imageUrl,
    }))

  return [...pendingVideos, ...pendingPhotos].sort((a, b) => a.submittedAt.localeCompare(b.submittedAt))
}

export async function getQueue(query: ModerationQuery): Promise<PagedResult<ModerationItemDto>> {
  await delay()
  const items = queue().filter((item) => (query.kind === 'all' ? true : item.kind === query.kind))
  return mockPage(items, query)
}

function setStatus(kind: 'photo' | 'video', id: number, status: ModerationStatus) {
  if (kind === 'video') {
    const video = videos.find((v) => v.id === id)
    if (video) video.status = status
    return
  }
  const photo = photos.find((p) => p.id === id)
  if (photo) photo.status = status
}

export async function approve(kind: 'photo' | 'video', id: number): Promise<void> {
  await delay(150)
  setStatus(kind, id, ModerationStatus.approved)
}

export async function reject(kind: 'photo' | 'video', id: number, reason: string): Promise<void> {
  await delay(150)
  void reason
  setStatus(kind, id, ModerationStatus.rejected)
}
