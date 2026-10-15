import type { AppBadgeTone } from '@/components/app-badge'

export const BandVerificationStatus = {
  pending: 0,
  approved: 1,
  rejected: 2,
} as const
export type BandVerificationStatus = (typeof BandVerificationStatus)[keyof typeof BandVerificationStatus]

export const allBandVerificationStatuses: BandVerificationStatus[] = [
  BandVerificationStatus.pending,
  BandVerificationStatus.approved,
  BandVerificationStatus.rejected,
]

export const bandVerificationStatusKey: Record<BandVerificationStatus, string> = {
  [BandVerificationStatus.pending]: 'pending',
  [BandVerificationStatus.approved]: 'approved',
  [BandVerificationStatus.rejected]: 'rejected',
}

export const bandVerificationStatusTone: Record<BandVerificationStatus, AppBadgeTone> = {
  [BandVerificationStatus.pending]: 'warning',
  [BandVerificationStatus.approved]: 'success',
  [BandVerificationStatus.rejected]: 'danger',
}

export const adminBandsPath = '/admin/bandas'
