import { useEffect } from 'react'

import { AppSkeleton } from '@/components/app-skeleton'

export function ExternalRedirect({ to }: { to: string }) {
  useEffect(() => {
    window.location.replace(to)
  }, [to])

  return <AppSkeleton preset="page" />
}
