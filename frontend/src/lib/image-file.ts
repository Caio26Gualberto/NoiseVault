export const avatarMaxBytes = 5 * 1024 * 1024
export const bannerMaxBytes = 10 * 1024 * 1024
export const bannerMinWidth = 800
export const acceptedImageTypes = ['image/jpeg', 'image/png', 'image/webp']

export type ImageCheck = 'ok' | 'unsupported' | 'tooLarge'

export function checkImageFile(file: File, maxBytes: number): ImageCheck {
  if (!acceptedImageTypes.includes(file.type)) return 'unsupported'
  if (file.size > maxBytes) return 'tooLarge'
  return 'ok'
}
