export const MediaStatus = { uploading: 0, processing: 1, ready: 2, failed: 3 } as const
export type MediaStatus = (typeof MediaStatus)[keyof typeof MediaStatus]
