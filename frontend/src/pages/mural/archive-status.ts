import type { AppBadgeTone } from '@/components/app-badge'

export const ArchiveEntryStatus = { published: 0, pending: 1, rejected: 2 } as const
export type ArchiveEntryStatus = (typeof ArchiveEntryStatus)[keyof typeof ArchiveEntryStatus]

export const archiveStatusKey: Record<ArchiveEntryStatus, string> = {
  [ArchiveEntryStatus.published]: 'published',
  [ArchiveEntryStatus.pending]: 'pending',
  [ArchiveEntryStatus.rejected]: 'rejected',
}

export const archiveStatusTone: Record<ArchiveEntryStatus, AppBadgeTone> = {
  [ArchiveEntryStatus.published]: 'success',
  [ArchiveEntryStatus.pending]: 'warning',
  [ArchiveEntryStatus.rejected]: 'neutral',
}

export const allArchiveStatuses = [ArchiveEntryStatus.pending, ArchiveEntryStatus.published, ArchiveEntryStatus.rejected] as ArchiveEntryStatus[]

export const mySubmissionsPath = '/mural/envios'
export const submitArchivePath = '/mural/enviar'
export const adminArchivePath = '/admin/mural'
