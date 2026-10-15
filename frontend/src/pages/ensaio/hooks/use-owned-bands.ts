import { useEffect, useState } from 'react'

import { getOwnedBands } from '@/pages/bandas/services/bandas-service'
import type { BandSummaryDto } from '@/pages/bandas/services/bandas-service'

export function useOwnedBands() {
  const [bands, setBands] = useState<BandSummaryDto[]>([])
  const [loaded, setLoaded] = useState(false)

  useEffect(() => {
    let cancelled = false
    getOwnedBands()
      .then((data) => {
        if (!cancelled) setBands(data)
      })
      .catch(() => undefined)
      .finally(() => {
        if (!cancelled) setLoaded(true)
      })
    return () => {
      cancelled = true
    }
  }, [])

  return { bands, loaded }
}
