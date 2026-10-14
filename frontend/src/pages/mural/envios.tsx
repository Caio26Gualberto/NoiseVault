import { CassetteTapeIcon, PencilIcon, PlusIcon, Trash2Icon } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { AppBadge } from '@/components/app-badge'
import { AppButton, AppIconButton } from '@/components/app-button'
import { useAppConfirm } from '@/components/app-confirm'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppPageHeader } from '@/components/app-page-header'
import { AppPagination, defaultPageSize } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { formatRelative } from '@/lib/format'
import { MediaKind } from '@/pages/feed/media-kind'

import { formatArchiveDate } from './archive-date'
import { ArchiveEntryStatus, archiveStatusKey, archiveStatusTone, submitArchivePath } from './archive-status'
import { deleteArchiveSubmission, getMySubmissions } from './services/mural-service'
import type { ArchiveEntryDto } from './services/mural-service'

export function MuralEnviosPage() {
  const { t } = useTranslation('mural')
  const confirm = useAppConfirm()
  const [page, setPage] = useState(1)
  const [version, setVersion] = useState(0)
  const [result, setResult] = useState<PagedResult<ArchiveEntryDto>>(() => emptyPage(defaultPageSize))
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    getMySubmissions({ page, pageSize: defaultPageSize })
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .catch(() => {
        if (!cancelled) setResult(emptyPage(defaultPageSize))
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, version])

  const remove = (entry: ArchiveEntryDto) => {
    const pending = entry.status === ArchiveEntryStatus.pending
    confirm({
      title: t(pending ? 'submissions.withdrawTitle' : 'submissions.deleteTitle'),
      message: t(pending ? 'submissions.withdrawMessage' : 'submissions.deleteMessage'),
      confirmLabel: t(pending ? 'submissions.withdraw' : 'submissions.delete'),
      danger: true,
      onConfirm: async () => {
        await deleteArchiveSubmission(entry.id)
        setVersion((value) => value + 1)
      },
    })
  }

  return (
    <div className="app-page-enter mx-auto flex max-w-3xl flex-col">
      <AppPageHeader
        title={t('submissions.title')}
        description={t('submissions.description')}
        actions={
          <AppButton asChild>
            <Link to={submitArchivePath}>
              <PlusIcon />
              {t('submit')}
            </Link>
          </AppButton>
        }
      />

      {loading ? (
        <AppSkeleton preset="list" count={4} />
      ) : result.items.length === 0 ? (
        <AppEmptyState icon={CassetteTapeIcon} title={t('submissions.emptyTitle')} description={t('submissions.emptyDescription')} />
      ) : (
        <ul className="flex flex-col divide-y rounded-xl border">
          {result.items.map((entry) => {
            const cover = entry.cover ? (entry.cover.kind === MediaKind.video ? entry.cover.posterUrl : entry.cover.url) : null
            const pending = entry.status === ArchiveEntryStatus.pending
            return (
              <li key={entry.id} className="flex gap-3 p-3 sm:p-4">
                <Link to={`/mural/${entry.id}`} className="size-16 shrink-0 overflow-hidden rounded-lg bg-muted sm:size-20">
                  {cover ? (
                    <img src={cover} alt="" loading="lazy" className="size-full object-cover" />
                  ) : (
                    <span className="flex size-full items-center justify-center text-muted-foreground">
                      <CassetteTapeIcon className="size-6" />
                    </span>
                  )}
                </Link>
                <div className="flex min-w-0 flex-1 flex-col gap-1">
                  <span className="flex flex-wrap items-center gap-2">
                    <AppBadge tone={archiveStatusTone[entry.status]}>{t(`status.${archiveStatusKey[entry.status]}`)}</AppBadge>
                    <span className="app-meta">{formatArchiveDate(entry)}</span>
                  </span>
                  <Link to={`/mural/${entry.id}`} className="truncate font-medium hover:text-primary">
                    {entry.title}
                  </Link>
                  {entry.status === ArchiveEntryStatus.rejected && entry.rejectionReason ? (
                    <p className="text-sm text-muted-foreground">{t('submissions.reason', { reason: entry.rejectionReason })}</p>
                  ) : null}
                  <span className="app-meta">{t('submissions.sentAt', { when: formatRelative(entry.createdAt) })}</span>
                </div>
                <div className="flex shrink-0 items-start gap-1">
                  {pending ? (
                    <AppIconButton label={t('submissions.edit')} size="sm" asChild>
                      <Link to={`/mural/${entry.id}/editar`}>
                        <PencilIcon />
                      </Link>
                    </AppIconButton>
                  ) : null}
                  {entry.status !== ArchiveEntryStatus.published ? (
                    <AppIconButton label={t(pending ? 'submissions.withdraw' : 'submissions.delete')} size="sm" onClick={() => remove(entry)}>
                      <Trash2Icon />
                    </AppIconButton>
                  ) : null}
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
