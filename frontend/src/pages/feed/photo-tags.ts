import type { AppPhotoTag, AppPhotoTagging } from '@/components/app-media-viewer'

import { addPhotoTag, removePhotoTag } from './services/media-service'
import type { MediaDto } from './services/media-service'
import { searchTaggablePeople } from './services/mentions-service'

export function toAppTags(media: MediaDto): AppPhotoTag[] {
  return (media.tags ?? []).map((tag) => ({
    id: tag.id,
    x: tag.x,
    y: tag.y,
    name: tag.user?.name ?? tag.name ?? '',
    href: tag.user ? `/perfil/${tag.user.nickname}` : null,
    canRemove: tag.canRemove,
  }))
}

export function photoTagging(onChanged: () => Promise<void> | void): AppPhotoTagging {
  return {
    searchPeople: searchTaggablePeople,
    onAdd: async (itemId, x, y, target) => {
      await addPhotoTag(Number(itemId), { x, y, ...target })
      await onChanged()
    },
    onRemove: async (tagId) => {
      await removePhotoTag(tagId)
      await onChanged()
    },
  }
}
