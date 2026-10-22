import { apiGet } from '@/api/base-service'
import type { PagedRequest, PagedResult } from '@/api/types'

export type NewsScope = 'today' | 'earlier'

export type NewsArticleDto = {
  id: number
  source: number
  title: string
  url: string
  imageUrl: string | null
  author: string | null
  publishedAt: string
}

export type NewsListQuery = PagedRequest & {
  scope: NewsScope
}

export const sourceName = 'Whiplash.Net'
export const sourceUrl = 'https://whiplash.net'

export function getNews(query: NewsListQuery): Promise<PagedResult<NewsArticleDto>> {
  return apiGet<PagedResult<NewsArticleDto>>('/api/news', { page: query.page, pageSize: query.pageSize, scope: query.scope })
}
