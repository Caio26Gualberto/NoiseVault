import { MediaKind } from '@/pages/feed/media-kind'
import type { MediaDto } from '@/pages/feed/services/media-service'

export type ManagedMedia = {
  key: string
  kind: 'photo' | 'video'
  previewUrl: string | null
  localUrl: boolean
  mediaId: number | null
  caption: string
  state: 'uploading' | 'ready' | 'error'
  progress: number | null
  saved: boolean
  controller: AbortController | null
}

export const archiveMaxMedia = 100

export function fromSaved(media: MediaDto, caption: string | null): ManagedMedia {
  const video = media.kind === MediaKind.video
  return {
    key: `saved-${media.id}`,
    kind: video ? 'video' : 'photo',
    previewUrl: video ? media.posterUrl : media.url,
    localUrl: false,
    mediaId: media.id,
    caption: caption ?? '',
    state: 'ready',
    progress: null,
    saved: true,
    controller: null,
  }
}
