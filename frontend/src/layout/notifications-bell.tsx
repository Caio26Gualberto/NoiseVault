import { BellIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { cn } from '@/lib/utils'
import { NotificationsPanel } from '@/pages/notificacoes/components/notifications-panel'

import { useNotifications } from './use-notifications'

export function NotificationsBell() {
  const { t } = useTranslation()
  const { unreadCount: count, panelOpen } = useNotifications()

  const label = count > 0 ? `${t('layout:bell.label')} · ${t('layout:bell.unread', { count })}` : t('layout:bell.label')

  return (
    <Tooltip>
      <NotificationsPanel>
        <TooltipTrigger asChild>
          <button
            type="button"
            aria-label={label}
            className={cn(
              'relative flex size-9 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:bg-muted hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none',
              panelOpen && 'bg-muted text-foreground',
            )}
          >
            <BellIcon className={cn('size-5', panelOpen && 'fill-current')} />
            {count > 0 ? (
              <span className="absolute top-1 right-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-primary px-1 text-[10px] font-bold tabular-nums text-primary-foreground">
                {count > 9 ? '9+' : count}
              </span>
            ) : null}
          </button>
        </TooltipTrigger>
      </NotificationsPanel>
      {panelOpen ? null : <TooltipContent>{label}</TooltipContent>}
    </Tooltip>
  )
}
