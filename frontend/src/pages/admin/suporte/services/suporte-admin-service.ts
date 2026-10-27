import { apiGet, apiPost, apiPut } from '@/api/base-service'
import type { PagedRequest, PagedResult } from '@/api/types'
import type { ProfileSummaryDto } from '@/pages/perfil/services/perfil-service'
import type { SupportMessageDto, SupportTicketDto } from '@/pages/ajuda/services/suporte-service'
import type { SupportArea, SupportTag, SupportTicketPriority, SupportTicketStatus } from '@/pages/ajuda/support-enums'

export type SupportTicketAdminDto = SupportTicketDto & {
  reporter: ProfileSummaryDto
  originPath: string | null
  userAgent: string | null
  viewportWidth: number | null
  viewportHeight: number | null
  internalNote: string | null
  canEdit: boolean
  canReply: boolean
}

export type SupportSummaryDto = {
  open: number
  inProgress: number
  resolved: number
  dismissed: number
}

export type SupportSort = 'activity' | 'newest' | 'priority'

export type SupportTicketListQuery = PagedRequest & {
  status: SupportTicketStatus | null
  tag: SupportTag | null
  area: SupportArea | null
  priority: SupportTicketPriority | null
  search: string
  sort: SupportSort
}

export type SupportTicketUpdateDto = {
  status: SupportTicketStatus
  priority: SupportTicketPriority
  internalNote: string | null
}

export function getTickets(query: SupportTicketListQuery) {
  return apiGet<PagedResult<SupportTicketAdminDto>>('/api/admin/support/tickets', query)
}

export function getSummary() {
  return apiGet<SupportSummaryDto>('/api/admin/support/summary')
}

export function getTicket(id: number) {
  return apiGet<SupportTicketAdminDto>(`/api/admin/support/tickets/${id}`)
}

export function getMessages(id: number, query: PagedRequest) {
  return apiGet<PagedResult<SupportMessageDto>>(`/api/admin/support/tickets/${id}/messages`, query)
}

export function replyAsStaff(id: number, text: string) {
  return apiPost<SupportMessageDto>(`/api/admin/support/tickets/${id}/messages`, { text })
}

export function updateTicket(id: number, input: SupportTicketUpdateDto) {
  return apiPut<SupportTicketAdminDto>(`/api/admin/support/tickets/${id}`, input, { toastSuccess: true })
}
