import { CheckIcon, ShieldCheckIcon, XIcon } from 'lucide-react'
import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { AppAvatar } from '@/components/app-avatar'
import { AppBadge } from '@/components/app-badge'
import { AppButton } from '@/components/app-button'
import { useAppConfirm } from '@/components/app-confirm'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppPageHeader } from '@/components/app-page-header'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { formatRelative } from '@/lib/format'

import { approve, getQueue, reject } from './services/moderacao-service'
import type { ModerationItemDto, ModerationQuery } from './services/moderacao-service'

export function ModeracaoPage() {
  const { t } = useTranslation('admin')
  const confirm = useAppConfirm()
  const [kind, setKind] = useState<ModerationQuery['kind']>('all')
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<ModerationItemDto>>(() => emptyPage(20))
  const [loading, setLoading] = useState(true)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    getQueue({ page, pageSize: 20, kind })
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, kind, version])

  const refresh = useCallback(() => setVersion((v) => v + 1), [])

  const changeKind = (value: ModerationQuery['kind']) => {
    setKind(value)
    setPage(1)
  }

  const onReject = (item: ModerationItemDto) =>
    confirm({
      title: t('reject.title'),
      message: t('reject.message', { title: item.title }),
      danger: true,
      confirmLabel: t('reject.confirm'),
      onConfirm: () => reject(item.kind, item.id, '').then(refresh),
    })

  return (
    <div className="app-page-enter mx-auto flex max-w-3xl flex-col">
      <AppPageHeader
        title={t('moderation.title')}
        description={t('moderation.description')}
        meta={t('moderation.pending', { count: result.totalCount })}
        actions={
          <Tabs value={kind} onValueChange={(value) => changeKind(value as ModerationQuery['kind'])}>
            <TabsList>
              <TabsTrigger value="all">{t('moderation.kinds.all')}</TabsTrigger>
              <TabsTrigger value="photo">{t('moderation.kinds.photo')}</TabsTrigger>
              <TabsTrigger value="video">{t('moderation.kinds.video')}</TabsTrigger>
            </TabsList>
          </Tabs>
        }
      />

      {loading ? (
        <AppSkeleton preset="list" count={4} />
      ) : result.items.length === 0 ? (
        <AppEmptyState icon={ShieldCheckIcon} title={t('moderation.empty.title')} description={t('moderation.empty.description')} />
      ) : (
        <ul className="flex flex-col gap-3">
          {result.items.map((item) => (
            <li key={`${item.kind}-${item.id}`} className="flex flex-col gap-3 rounded-xl border bg-card p-3 sm:flex-row sm:items-center">
              <img src={item.previewUrl} alt="" className="aspect-video w-full rounded-lg bg-muted object-cover sm:w-40" />
              <div className="flex min-w-0 flex-1 flex-col gap-1">
                <div className="flex flex-wrap items-center gap-2">
                  <AppBadge tone="neutral">{t(`moderation.kinds.${item.kind}`)}</AppBadge>
                  <span className="app-meta">{formatRelative(item.submittedAt)}</span>
                </div>
                <p className="font-medium leading-snug">{item.title}</p>
                {item.subtitle ? <p className="text-sm text-muted-foreground">{item.subtitle}</p> : null}
                <Link to={`/perfil/${item.author.nickname}`} className="mt-1 flex items-center gap-2">
                  <AppAvatar name={item.author.name} src={item.author.avatarUrl} size="sm" />
                  <span className="app-meta">{item.author.name}</span>
                </Link>
              </div>
              <div className="flex gap-2 sm:flex-col">
                <AppButton size="sm" onClick={() => void approve(item.kind, item.id).then(refresh)} className="flex-1">
                  <CheckIcon />
                  {t('approve')}
                </AppButton>
                <AppButton size="sm" intent="danger" onClick={() => onReject(item)} className="flex-1">
                  <XIcon />
                  {t('reject.action')}
                </AppButton>
              </div>
            </li>
          ))}
        </ul>
      )}

      <AppPagination page={result.page} totalPages={result.totalPages} totalCount={result.totalCount} onChange={setPage} loading={loading} />
    </div>
  )
}
