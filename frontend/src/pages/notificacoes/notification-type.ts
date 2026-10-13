export const NotificationType = { simple: 0, banner: 1, popup: 2, bandInvite: 3 } as const
export type NotificationType = (typeof NotificationType)[keyof typeof NotificationType]
