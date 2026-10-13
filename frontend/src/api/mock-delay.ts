import type { PagedRequest, PagedResult } from './types'

export function delay(ms = 350): Promise<void> {
  return new Promise((resolve) => window.setTimeout(resolve, ms))
}

export function mockPage<T>(items: T[], query: PagedRequest): PagedResult<T> {
  const pageSize = Math.min(Math.max(query.pageSize, 1), 100)
  const totalPages = Math.max(Math.ceil(items.length / pageSize), 1)
  const page = Math.min(Math.max(query.page, 1), totalPages)
  const start = (page - 1) * pageSize

  return {
    items: items.slice(start, start + pageSize),
    page,
    pageSize,
    totalCount: items.length,
    totalPages: items.length === 0 ? 0 : totalPages,
    hasPrevious: page > 1,
    hasNext: page < totalPages,
  }
}
