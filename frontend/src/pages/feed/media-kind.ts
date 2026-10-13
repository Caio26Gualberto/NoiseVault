export const MediaKind = { photo: 0, video: 1 } as const
export type MediaKind = (typeof MediaKind)[keyof typeof MediaKind]
