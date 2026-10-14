import { BellIcon, CheckCheckIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { AppIconButton } from '@/components/app-button'
import { useAppConfirm } from '@/components/app-confirm'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle } from '@/components/ui/sheet'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { useNotifications } from '@/layout/use-notifications'

import { useNotificationsPanel } from '../hooks/use-notifications-panel'
import { markAllRead, markRead, remove } from '../services/notificacoes-service'
import type { NotificationDto } from '../services/notificacoes-service'
import { BandInvitations } from './band-invitations'
import { NotificationItem } from './notification-item'

export function NotificationsPanel() {
  const { panelOpen, setPanelOpen } = useNotifications()

  return (
    <Sheet open={panelOpen} onOpenChange={setPanelOpen}>
      <SheetContent side="right" onOpenAutoFocus={(event) => event.preventDefault()} className="w-full gap-0 p-0 data-[side=right]:w-full data-[side=right]:sm:max-w-md">
        <PanelBody onClose={() => setPanelOpen(false)} />
      </SheetContent>
    </Sheet>
  )
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
      <SheetHeader className="flex-row items-center gap-2 border-b pr-12">
        <div className="flex min-w-0 flex-1 flex-col">
          <SheetTitle className="app-h3">{t('title')}</SheetTitle>
          <SheetDescription className="sr-only">{t('description')}</SheetDescription>
        </div>
        <AppIconButton label={t('markAllRead')} size="sm" disabled={unreadCount === 0} onClick={() => void markAllRead().then(afterChange)}>
          <CheckCheckIcon />
        </AppIconButton>
      </SheetHeader>

      <div className="flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto p-4">
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
          <ul className="-mx-4 flex flex-col divide-y border-y">
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
