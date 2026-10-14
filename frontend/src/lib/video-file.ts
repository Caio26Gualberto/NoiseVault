export const videoMaxBytes = 500 * 1024 * 1024
export const videoMaxSeconds = 600
export const postPhotoMaxBytes = 15 * 1024 * 1024

export type VideoCheck = 'ok' | 'unsupported' | 'tooLarge' | 'tooLong'

const typesByExtension: Record<string, string> = {
  mp4: 'video/mp4',
  m4v: 'video/mp4',
  mov: 'video/quicktime',
  webm: 'video/webm',
  mkv: 'video/x-matroska',
  avi: 'video/x-msvideo',
  '3gp': 'video/3gpp',
  mpg: 'video/mpeg',
  mpeg: 'video/mpeg',
}

export function videoContentType(file: File): string {
  if (file.type.startsWith('video/')) return file.type
  const extension = file.name.split('.').pop()?.toLowerCase() ?? ''
  return typesByExtension[extension] ?? 'video/mp4'
}

function isVideo(file: File): boolean {
  if (file.type) return file.type.startsWith('video/')
  const extension = file.name.split('.').pop()?.toLowerCase() ?? ''
  return extension in typesByExtension
}

function readDuration(file: File): Promise<number | null> {
  return new Promise((resolve) => {
    const url = URL.createObjectURL(file)
    const video = document.createElement('video')
    let settled = false

    const finish = (value: number | null) => {
      if (settled) return
      settled = true
      URL.revokeObjectURL(url)
      video.removeAttribute('src')
      resolve(value)
    }

    video.preload = 'metadata'
    video.onloadedmetadata = () => finish(Number.isFinite(video.duration) ? video.duration : null)
    video.onerror = () => finish(null)
    setTimeout(() => finish(null), 5000)
    video.src = url
  })
}

export type VideoLimits = { maxBytes: number; maxSeconds: number }

export const postVideoLimits: VideoLimits = { maxBytes: videoMaxBytes, maxSeconds: videoMaxSeconds }
export const archiveVideoLimits: VideoLimits = { maxBytes: 4 * 1024 * 1024 * 1024, maxSeconds: 2 * 60 * 60 }
export const archivePhotoMaxBytes = 40 * 1024 * 1024

export async function checkVideoFile(file: File, limits: VideoLimits = postVideoLimits): Promise<VideoCheck> {
  if (!isVideo(file)) return 'unsupported'
  if (file.size > limits.maxBytes) return 'tooLarge'
  const duration = await readDuration(file)
  if (duration !== null && duration > limits.maxSeconds + 1) return 'tooLong'
  return 'ok'
}
