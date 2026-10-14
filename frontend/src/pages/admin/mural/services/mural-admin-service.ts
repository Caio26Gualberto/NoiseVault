import { apiGet, apiPost } from '@/api/base-service'
import type { PagedRequest, PagedResult } from '@/api/types'
import type { ArchiveEntryStatus } from '@/pages/mural/archive-status'
import type { ArchiveEntryDto } from '@/pages/mural/services/mural-service'

export type ArchiveSubmissionQuery = PagedRequest & {
  status: ArchiveEntryStatus | null
  search: string
}

export type ArchiveSubmissionSummaryDto = {
  pending: number
  published: number
  rejected: number
}

export function getSubmissions(query: ArchiveSubmissionQuery): Promise<PagedResult<ArchiveEntryDto>> {
  return apiGet<PagedResult<ArchiveEntryDto>>('/api/admin/archive/submissions', {
    page: query.page,
    pageSize: query.pageSize,
    status: query.status ?? undefined,
    search: query.search || undefined,
  })
}

export function getSubmissionSummary(): Promise<ArchiveSubmissionSummaryDto> {
  return apiGet<ArchiveSubmissionSummaryDto>('/api/admin/archive/submissions/summary')
}

export function approveSubmission(id: number): Promise<ArchiveEntryDto> {
  return apiPost<ArchiveEntryDto>(`/api/admin/archive/${id}/approve`, undefined, { toastSuccess: true })
}

export function rejectSubmission(id: number, reason: string): Promise<ArchiveEntryDto> {
  return apiPost<ArchiveEntryDto>(`/api/admin/archive/${id}/reject`, { reason }, { toastSuccess: true })
}
