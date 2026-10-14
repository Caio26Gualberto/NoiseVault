import { apiGet } from '@/api/base-service'
import type { PagedResult } from '@/api/types'

import type { CityDto } from '../city'

export const citySearchMinLength = 2

export async function searchCities(search: string, pageSize = 8) {
  const result = await apiGet<PagedResult<CityDto>>('/api/cities', { search, page: 1, pageSize }, { toastError: false })
  return result.items
}
