import { BellIcon, CheckCheckIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import { AppIconButton } from '@/components/app-button'
import { useAppConfirm } from '@/components/app-confirm'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { useNotifications } from '@/layout/use-notifications'

import { useNotificationsPanel } from '../hooks/use-notifications-panel'
import { markAllRead, markRead, remove } from '../services/notificacoes-service'
import type { NotificationDto } from '../services/notificacoes-service'
import { BandInvitations } from './band-invitations'
import { NotificationItem } from './notification-item'

export function NotificationsPanel({ children }: { children: ReactNode }) {
  const { panelOpen, setPanelOpen } = useNotifications()

  return (
    <Popover open={panelOpen} onOpenChange={setPanelOpen}>
      <PopoverTrigger asChild>{children}</PopoverTrigger>
      <PopoverContent
        align="end"
        sideOffset={8}
        collisionPadding={8}
        onOpenAutoFocus={(event) => event.preventDefault()}
        onInteractOutside={(event) => {
          if (keepsOpen(event.target)) event.preventDefault()
        }}
        className="max-h-[min(36rem,calc(100dvh-5rem))] w-[min(24rem,calc(100vw-1rem))] gap-0 overflow-hidden p-0"
      >
        <PanelBody onClose={() => setPanelOpen(false)} />
      </PopoverContent>
    </Popover>
  )
}

function keepsOpen(target: EventTarget | null) {
  return target instanceof Element && target.closest('[role="alertdialog"], [data-sonner-toaster]') !== null
}

function PanelBody({ onClose }: { onClose: () => void }) {
  const { t } = useTranslation('notificacoes')
  const confirm = useAppConfirm()
  const { unreadCount, refresh: refreshCount } = useNotifications()
  const panel = useNotificationsPanel()

  const afterChange = () => {
    panel.refresh()
    void refreshCount()
  }

  const onOpen = (item: NotificationDto) => {
    if (item.readAt === null) void markRead(item.id).then(() => refreshCount()).catch(() => undefined)
    onClose()
  }

  const onMarkRead = (item: NotificationDto) => void markRead(item.id).then(afterChange)

  const onRemove = (item: NotificationDto) =>
    confirm({
      title: t('remove.title'),
      message: t('remove.message'),
      danger: true,
      confirmLabel: t('remove.confirm'),
      onConfirm: () => remove(item.id).then(afterChange),
    })

  return (
    <>
      <div className="flex items-center gap-2 border-b px-4 py-3">
        <h2 className="app-h3 min-w-0 flex-1">{t('title')}</h2>
        <AppIconButton label={t('markAllRead')} size="sm" disabled={unreadCount === 0} onClick={() => void markAllRead().then(afterChange)}>
          <CheckCheckIcon />
        </AppIconButton>
      </div>

      <div className="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto p-3">
        <BandInvitations onNavigate={onClose} />

        <Tabs value={panel.unreadOnly ? 'unread' : 'all'} onValueChange={(value) => panel.changeUnreadOnly(value === 'unread')}>
          <TabsList>
            <TabsTrigger value="all">{t('filters.all')}</TabsTrigger>
            <TabsTrigger value="unread">{t('filters.unread')}</TabsTrigger>
          </TabsList>
        </Tabs>

        {panel.loading && panel.result.items.length === 0 ? (
          <AppSkeleton preset="list" count={5} />
        ) : panel.result.items.length === 0 ? (
          <AppEmptyState
            icon={BellIcon}
            title={panel.unreadOnly ? t('empty.unreadTitle') : t('empty.title')}
            description={panel.unreadOnly ? t('empty.unreadDescription') : t('empty.description')}
          />
        ) : (
          <ul className="-mx-3 flex flex-col divide-y border-y">
            {panel.result.items.map((item) => (
              <NotificationItem key={item.id} item={item} onOpen={onOpen} onMarkRead={onMarkRead} onRemove={onRemove} />
            ))}
          </ul>
        )}

        <AppPagination
          page={panel.result.page}
          totalPages={panel.result.totalPages}
          totalCount={panel.result.totalCount}
          onChange={panel.loadMore}
          loading={panel.loading}
          mode="loadMore"
        />
      </div>
    </>
  )
}
