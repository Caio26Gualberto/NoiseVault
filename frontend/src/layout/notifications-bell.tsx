import { BellIcon } from 'lucide-react'
import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useLocation } from 'react-router-dom'

import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { usePolling } from '@/hooks/use-polling'
import { getUnreadCount } from '@/pages/notificacoes/services/notificacoes-service'

export function NotificationsBell() {
  const { t } = useTranslation()
  const location = useLocation()
  const [count, setCount] = useState(0)

  const refresh = useCallback(async () => {
    const next = await getUnreadCount().catch(() => null)
    if (next !== null) setCount(next)
  }, [])

  useEffect(() => {
    void refresh()
  }, [refresh, location.pathname])

  usePolling(refresh, 30000)

  const label = count > 0 ? `${t('layout:bell.label')} · ${t('layout:bell.unread', { count })}` : t('layout:bell.label')

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <Link
          to="/notificacoes"
          aria-label={label}
          className="relative flex size-9 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:bg-muted hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
        >
          <BellIcon className="size-5" />
          {count > 0 ? (
            <span className="absolute top-1 right-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-primary px-1 text-[10px] font-bold tabular-nums text-primary-foreground">
              {count > 9 ? '9+' : count}
            </span>
          ) : null}
        </Link>
      </TooltipTrigger>
      <TooltipContent>{label}</TooltipContent>
    </Tooltip>
  )
}
