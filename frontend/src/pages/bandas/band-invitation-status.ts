export const BandInvitationStatus = { pending: 0, accepted: 1, declined: 2, revoked: 3 } as const
export type BandInvitationStatus = (typeof BandInvitationStatus)[keyof typeof BandInvitationStatus]
