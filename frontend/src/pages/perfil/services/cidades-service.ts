import { apiGet } from '@/api/base-service'
import type { PagedResult } from '@/api/types'

import type { CityDto } from '../city'

export const citySearchMinLength = 2

export async function searchCities(search: string, pageSize = 8) {
  const result = await apiGet<PagedResult<CityDto>>('/api/cities', { search, page: 1, pageSize }, { toastError: false })
  return result.items
}

let featured: Promise<CityDto[]> | null = null

export function featuredCities() {
  featured ??= apiGet<CityDto[]>('/api/cities/featured', undefined, { toastError: false }).catch((error: unknown) => {
    featured = null
    throw error
  })
  return featured
}
