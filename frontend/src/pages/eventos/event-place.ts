import { useCallback } from 'react'

import { AbcCity, useAbcCityLabel } from '@/pages/perfil/cities'

type Placed = { city: AbcCity; cityName: string | null }

export function useEventPlace() {
  const cityLabel = useAbcCityLabel()
  return useCallback((event: Placed) => (event.city === AbcCity.other && event.cityName ? event.cityName : cityLabel(event.city)), [cityLabel])
}
