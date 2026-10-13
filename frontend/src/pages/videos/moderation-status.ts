import type { AppBadgeTone } from '@/components/app-badge'

export const ModerationStatus = { pending: 0, approved: 1, rejected: 2 } as const
export type ModerationStatus = (typeof ModerationStatus)[keyof typeof ModerationStatus]

export const moderationStatusKey: Record<ModerationStatus, string> = {
  [ModerationStatus.pending]: 'pending',
  [ModerationStatus.approved]: 'approved',
  [ModerationStatus.rejected]: 'rejected',
}

export const moderationStatusTone: Record<ModerationStatus, AppBadgeTone> = {
  [ModerationStatus.pending]: 'warning',
  [ModerationStatus.approved]: 'success',
  [ModerationStatus.rejected]: 'danger',
}
