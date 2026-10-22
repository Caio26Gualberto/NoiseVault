import { apiGet, apiPost } from '@/api/base-service'
import type { PagedRequest, PagedResult } from '@/api/types'

export type RecurringJobDto = {
  id: string
  name?: string | null
  cron: string | null
  queue: string | null
  lastExecution: string | null
  nextExecution: string | null
  lastJobId?: string | null
  lastJobState: string | null
  lastDurationMs?: number | null
  lastResult?: string | null
  lastError?: string | null
  lastErrorDetails?: string | null
  error?: string | null
  canTrigger?: boolean
}

export type FailedJobDto = {
  jobId: string
  name: string | null
  recurringJobId: string | null
  queue: string | null
  failedAt: string | null
  exceptionType: string | null
  exceptionMessage: string | null
  exceptionDetails: string | null
}

export type TriggeredRecurringJobDto = {
  recurringJobId: string
  jobId: string
}

export function getRecurringJobs(quiet = false): Promise<RecurringJobDto[]> {
  return apiGet<RecurringJobDto[]>('/api/admin/jobs/recurring', undefined, { toastError: !quiet })
}

export function triggerRecurringJob(id: string): Promise<TriggeredRecurringJobDto> {
  return apiPost<TriggeredRecurringJobDto>(`/api/admin/jobs/recurring/${encodeURIComponent(id)}/trigger`, undefined, { toastSuccess: true })
}

export function getFailedJobs(query: PagedRequest): Promise<PagedResult<FailedJobDto>> {
  return apiGet<PagedResult<FailedJobDto>>('/api/admin/jobs/failures', { page: query.page, pageSize: query.pageSize })
}
