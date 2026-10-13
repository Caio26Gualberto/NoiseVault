export const BandMemberRole = { owner: 0, member: 1 } as const
export type BandMemberRole = (typeof BandMemberRole)[keyof typeof BandMemberRole]
