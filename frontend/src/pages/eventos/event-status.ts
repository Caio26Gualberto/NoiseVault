export const EventStatus = { draft: 0, published: 1, cancelled: 2 } as const
export type EventStatus = (typeof EventStatus)[keyof typeof EventStatus]
