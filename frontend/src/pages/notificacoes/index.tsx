import { BellIcon, CheckCheckIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { AppButton } from '@/components/app-button'
import { useAppConfirm } from '@/components/app-confirm'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppPageHeader } from '@/components/app-page-header'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { useNotifications } from '@/layout/use-notifications'

import { BandInvitations } from './components/band-invitations'
import { NotificationItem } from './components/notification-item'
import { useNotificacoesPage } from './hooks/use-notificacoes-page'
import { markAllRead, markRead, remove } from './services/notificacoes-service'
import type { NotificationDto } from './services/notificacoes-service'

export function NotificacoesPage() {
  const { t } = useTranslation('notificacoes')
  const confirm = useAppConfirm()
  const { unreadCount, refresh: refreshCount } = useNotifications()
  const page = useNotificacoesPage()

  const afterChange = () => {
    page.refresh()
    void refreshCount()
  }

  const onOpen = (item: NotificationDto) => {
    if (item.readAt === null) void markRead(item.id).catch(() => undefined)
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
    <div className="app-page-enter mx-auto flex max-w-2xl flex-col">
      <AppPageHeader
        title={t('title')}
        description={t('description')}
        actions={
          <AppButton intent="neutral" size="sm" disabled={unreadCount === 0} onClick={() => void markAllRead().then(afterChange)}>
            <CheckCheckIcon />
            {t('markAllRead')}
          </AppButton>
        }
      />

      <BandInvitations />

      <Tabs value={page.unreadOnly ? 'unread' : 'all'} onValueChange={(value) => page.changeUnreadOnly(value === 'unread')} className="mb-4">
        <TabsList>
          <TabsTrigger value="all">{t('filters.all')}</TabsTrigger>
          <TabsTrigger value="unread">{t('filters.unread')}</TabsTrigger>
        </TabsList>
      </Tabs>

      {page.loading && page.result.items.length === 0 ? (
        <AppSkeleton preset="list" count={5} />
      ) : page.result.items.length === 0 ? (
        <AppEmptyState
          icon={BellIcon}
          title={page.unreadOnly ? t('empty.unreadTitle') : t('empty.title')}
          description={page.unreadOnly ? t('empty.unreadDescription') : t('empty.description')}
        />
      ) : (
        <ul className="flex flex-col divide-y rounded-xl border bg-card">
          {page.result.items.map((item) => (
            <NotificationItem key={item.id} item={item} onOpen={onOpen} onMarkRead={onMarkRead} onRemove={onRemove} />
          ))}
        </ul>
      )}

      <AppPagination
        page={page.result.page}
        totalPages={page.result.totalPages}
        totalCount={page.result.totalCount}
        onChange={page.setPage}
        loading={page.loading}
      />
    </div>
  )
}
