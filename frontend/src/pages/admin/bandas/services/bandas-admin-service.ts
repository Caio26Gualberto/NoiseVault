import { apiGet, apiPost } from '@/api/base-service'
import type { PagedRequest, PagedResult } from '@/api/types'
import type { BandVerificationStatus } from '@/pages/bandas/band-verification-status'
import type { BandProfileDto, BandVerificationDto } from '@/pages/bandas/services/bandas-service'

export type BandVerificationQuery = PagedRequest & {
  status: BandVerificationStatus | null
  search: string
}

export type BandVerificationSummaryDto = {
  pending: number
  approved: number
  rejected: number
}

export const bandVerificationReasonMinLength = 3
export const bandVerificationReasonMaxLength = 500

export function getVerifications(query: BandVerificationQuery): Promise<PagedResult<BandVerificationDto>> {
  return apiGet<PagedResult<BandVerificationDto>>('/api/admin/bands/verifications', {
    page: query.page,
    pageSize: query.pageSize,
    status: query.status ?? undefined,
    search: query.search || undefined,
  })
}

export function getVerificationSummary(): Promise<BandVerificationSummaryDto> {
  return apiGet<BandVerificationSummaryDto>('/api/admin/bands/verifications/summary')
}

export function approveVerification(id: number): Promise<BandVerificationDto> {
  return apiPost<BandVerificationDto>(`/api/admin/bands/verifications/${id}/approve`, undefined, { toastSuccess: true })
}

export function rejectVerification(id: number, reason: string): Promise<BandVerificationDto> {
  return apiPost<BandVerificationDto>(`/api/admin/bands/verifications/${id}/reject`, { reason }, { toastSuccess: true })
}

export function unverifyBand(bandId: number): Promise<BandProfileDto> {
  return apiPost<BandProfileDto>(`/api/admin/bands/${bandId}/unverify`, undefined, { toastSuccess: true })
}
