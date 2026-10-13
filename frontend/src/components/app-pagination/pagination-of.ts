import type { PagedResult } from '@/api/types'

export function paginationOf<T>(result: PagedResult<T>) {
  return { page: result.page, totalPages: result.totalPages, totalCount: result.totalCount }
}
