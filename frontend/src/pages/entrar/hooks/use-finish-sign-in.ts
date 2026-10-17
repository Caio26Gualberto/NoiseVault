import { useCallback } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'

import { homePath } from '@/auth/routes'
import { returnUrlFrom } from '@/ensaio/frame'

export function useFinishSignIn() {
  const navigate = useNavigate()
  const location = useLocation()

  return useCallback(() => {
    const external = returnUrlFrom(location.search)
    if (external) {
      window.location.assign(external)
      return
    }
    const from = (location.state as { from?: string } | null)?.from
    navigate(from ?? homePath, { replace: true })
  }, [location.search, location.state, navigate])
}
