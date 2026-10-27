import { useCallback, useEffect, useState } from 'react'

import { usePolling } from '@/hooks/use-polling'
import { MediaStatus } from '@/pages/feed/media-status'
import type { SupportTicketDto } from '@/pages/ajuda/services/suporte-service'

const processingPollMs = 5000

export function useTicketDetail<T extends SupportTicketDto>(id: number, load: (id: number) => Promise<T>) {
  const [ticket, setTicket] = useState<T | null>(null)
  const [loading, setLoading] = useState(true)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    load(id)
      .then((data) => {
        if (!cancelled) setTicket(data)
      })
      .catch(() => {
        if (!cancelled) setTicket(null)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [id, load, version])

  const refresh = useCallback(() => setVersion((v) => v + 1), [])

  const processing = ticket?.media?.some((media) => media.status === MediaStatus.processing || media.status === MediaStatus.uploading) ?? false

  usePolling(async () => {
    try {
      setTicket(await load(id))
    } catch {
      return
    }
  }, processing ? processingPollMs : 0)

  return { ticket, loading, refresh, replace: setTicket }
}
