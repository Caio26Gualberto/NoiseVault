import { useEffect, useState } from 'react'

import type { SignInMethodsDto } from '@/api/types'

import { getSignInMethods } from '../services/conta-service'

export function useSignInMethods() {
  const [methods, setMethods] = useState<SignInMethodsDto | null>(null)

  useEffect(() => {
    let cancelled = false

    getSignInMethods()
      .then((loaded) => {
        if (!cancelled) setMethods(loaded)
      })
      .catch(() => null)

    return () => {
      cancelled = true
    }
  }, [])

  return { methods, setMethods }
}
