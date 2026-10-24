export const BandPartnershipStatus = {
  pending: 0,
  accepted: 1,
  declined: 2,
  revoked: 3,
} as const
export type BandPartnershipStatus = (typeof BandPartnershipStatus)[keyof typeof BandPartnershipStatus]

export const BandPartnershipDirection = {
  outgoing: 0,
  incoming: 1,
} as const
export type BandPartnershipDirection = (typeof BandPartnershipDirection)[keyof typeof BandPartnershipDirection]
