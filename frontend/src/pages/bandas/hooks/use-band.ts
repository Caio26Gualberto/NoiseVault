import { useCallback, useEffect, useState } from 'react'

import { ApiCodes } from '@/api/api-codes'
import { feedback } from '@/api/feedback-sink'
import { ApiError } from '@/api/types'

import { getBandProfile } from '../services/bandas-service'
import type { BandProfileDto } from '../services/bandas-service'

export type BandState = { status: 'loading' } | { status: 'found'; band: BandProfileDto } | { status: 'notFound' } | { status: 'failed' }

export function useBand(slug: string) {
  const [state, setState] = useState<BandState>({ status: 'loading' })
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let cancelled = false
    getBandProfile(slug)
      .then((band) => {
        if (!cancelled) setState({ status: 'found', band })
      })
      .catch((error: unknown) => {
        if (cancelled) return
        if (error instanceof ApiError && error.code === ApiCodes.bandNotFound) {
          setState({ status: 'notFound' })
          return
        }
        if (error instanceof ApiError) feedback.error(error.code, error.backendMessage)
        setState({ status: 'failed' })
      })
    return () => {
      cancelled = true
    }
  }, [slug, version])

  const replace = useCallback((band: BandProfileDto) => setState({ status: 'found', band }), [])
  const reload = useCallback(() => setVersion((v) => v + 1), [])

  return { state, replace, reload }
}
