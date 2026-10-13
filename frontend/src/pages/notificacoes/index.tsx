import { BellIcon, CheckCheckIcon, Trash2Icon } from 'lucide-react'
import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { AppBadge } from '@/components/app-badge'
import { AppButton, AppIconButton } from '@/components/app-button'
import { useAppConfirm } from '@/components/app-confirm'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppPageHeader } from '@/components/app-page-header'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { cn } from '@/lib/utils'
import { formatRelative } from '@/lib/format'

import { getNotifications, markAllRead, markRead, remove, respondInvitation } from './services/notificacoes-service'
import type { NotificationDto } from './services/notificacoes-service'

export function NotificacoesPage() {
  const { t } = useTranslation('notificacoes')
  const confirm = useAppConfirm()
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<NotificationDto>>(() => emptyPage(20))
  const [loading, setLoading] = useState(true)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    getNotifications({ page, pageSize: 20 })
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, version])

  const refresh = useCallback(() => setVersion((v) => v + 1), [])

  const unread = result.items.filter((n) => n.readAt === null).length

  const onRemove = (item: NotificationDto) =>
    confirm({
      title: t('remove.title'),
      message: t('remove.message'),
      danger: true,
      confirmLabel: t('remove.confirm'),
      onConfirm: () => remove(item.id).then(refresh),
    })

  return (
    <div className="app-page-enter mx-auto flex max-w-2xl flex-col">
      <AppPageHeader
        title={t('title')}
        description={t('description')}
        actions={
          <AppButton intent="neutral" size="sm" disabled={unread === 0} onClick={() => void markAllRead().then(refresh)}>
            <CheckCheckIcon />
            {t('markAllRead')}
          </AppButton>
        }
      />

      {loading ? (
        <AppSkeleton preset="list" count={5} />
      ) : result.items.length === 0 ? (
        <AppEmptyState icon={BellIcon} title={t('empty.title')} description={t('empty.description')} />
      ) : (
        <ul className="flex flex-col divide-y rounded-xl border bg-card">
          {result.items.map((item) => {
            const isUnread = item.readAt === null
            return (
              <li key={item.id} className={cn('flex gap-3 p-4', isUnread && 'bg-primary/[0.04]')}>
                <span className="mt-2 flex w-2 shrink-0 justify-center">
                  {isUnread ? <span className="size-2 rounded-full bg-primary" aria-label={t('unread')} /> : null}
                </span>
                <div className="flex min-w-0 flex-1 flex-col gap-2">
                  <div className="flex flex-col gap-0.5">
                    <p className={cn('text-sm', isUnread ? 'font-semibold' : 'font-medium')}>{item.title}</p>
                    <p className="text-sm text-muted-foreground">{item.message}</p>
                    <span className="app-meta">{formatRelative(item.createdAt)}</span>
                  </div>

                  {item.imageUrl ? <img src={item.imageUrl} alt="" className="aspect-[3/1] w-full rounded-lg object-cover" /> : null}

                  {item.invitation ? (
                    item.invitation.status === 'pending' ? (
                      <div className="flex gap-2">
                        <AppButton size="sm" onClick={() => void respondInvitation(item.id, true).then(refresh)}>
                          {t('invite.accept')}
                        </AppButton>
                        <AppButton size="sm" intent="neutral" onClick={() => void respondInvitation(item.id, false).then(refresh)}>
                          {t('invite.decline')}
                        </AppButton>
                      </div>
                    ) : (
                      <AppBadge tone={item.invitation.status === 'accepted' ? 'success' : 'neutral'}>
                        {t(`invite.${item.invitation.status}`)}
                      </AppBadge>
                    )
                  ) : item.link ? (
                    <Link to={item.link} onClick={() => void markRead(item.id)} className="self-start text-[13px] font-medium text-primary hover:underline">
                      {t('open')}
                    </Link>
                  ) : null}
                </div>
                <div className="flex shrink-0 flex-col gap-1">
                  {isUnread ? (
                    <AppIconButton label={t('markRead')} size="sm" onClick={() => void markRead(item.id).then(refresh)}>
                      <CheckCheckIcon />
                    </AppIconButton>
                  ) : null}
                  <AppIconButton label={t('remove.action')} size="sm" onClick={() => onRemove(item)}>
                    <Trash2Icon />
                  </AppIconButton>
                </div>
              </li>
            )
          })}
        </ul>
      )}

      <AppPagination page={result.page} totalPages={result.totalPages} totalCount={result.totalCount} onChange={setPage} loading={loading} />
    </div>
  )
}
