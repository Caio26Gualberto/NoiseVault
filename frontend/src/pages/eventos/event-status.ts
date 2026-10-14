export const EventStatus = { published: 0, cancelled: 1 } as const
export type EventStatus = (typeof EventStatus)[keyof typeof EventStatus]
