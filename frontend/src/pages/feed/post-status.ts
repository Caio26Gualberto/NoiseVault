export const PostStatus = { published: 0, processing: 1, failed: 2 } as const
export type PostStatus = (typeof PostStatus)[keyof typeof PostStatus]
