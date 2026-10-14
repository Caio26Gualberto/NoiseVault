import { CameraIcon, CassetteTapeIcon, SearchIcon, VideoIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { AppAvatar } from '@/components/app-avatar'
import { AppBadge } from '@/components/app-badge'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppPageHeader } from '@/components/app-page-header'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { formatRelative } from '@/lib/format'
import { MediaKind } from '@/pages/feed/media-kind'
import { formatArchiveDate } from '@/pages/mural/archive-date'
import { ArchiveEntryStatus, allArchiveStatuses, archiveStatusKey, archiveStatusTone } from '@/pages/mural/archive-status'

import { useMuralAdminPage } from './hooks/use-mural-admin-page'
import type { ArchiveSubmissionSummaryDto } from './services/mural-admin-service'

const summaryCount: Record<ArchiveEntryStatus, (summary: ArchiveSubmissionSummaryDto) => number> = {
  [ArchiveEntryStatus.published]: (s) => s.published,
  [ArchiveEntryStatus.pending]: (s) => s.pending,
  [ArchiveEntryStatus.rejected]: (s) => s.rejected,
}

export function MuralAdminPage() {
  const { t } = useTranslation('mural')
  const page = useMuralAdminPage()

  return (
    <div className="app-page-enter flex flex-col">
      <AppPageHeader title={t('admin.title')} description={t('admin.description')} />

      <Tabs value={String(page.status)} onValueChange={(value) => page.changeStatus(Number(value) as ArchiveEntryStatus)} className="mb-4 max-w-full overflow-x-auto">
        <TabsList className="w-max">
          {allArchiveStatuses.map((status) => (
            <TabsTrigger key={status} value={String(status)} className="gap-1.5">
              {t(`admin.tabs.${archiveStatusKey[status]}`)}
              {page.summary ? <span className="text-muted-foreground tabular-nums">{summaryCount[status](page.summary)}</span> : null}
            </TabsTrigger>
          ))}
        </TabsList>
      </Tabs>

      <div className="relative mb-6">
        <SearchIcon className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
        <input
          type="search"
          value={page.search}
          onChange={(event) => page.changeSearch(event.target.value)}
          placeholder={t('admin.searchPlaceholder')}
          aria-label={t('admin.searchPlaceholder')}
          className="h-10 w-full rounded-lg border bg-transparent pr-3 pl-9 text-sm placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
        />
      </div>

      {page.loading ? (
        <AppSkeleton preset="list" count={5} />
      ) : page.items.length === 0 ? (
        <AppEmptyState
          icon={CassetteTapeIcon}
          title={t(page.status === ArchiveEntryStatus.pending ? 'admin.emptyPendingTitle' : 'admin.emptyTitle')}
          description={t(page.status === ArchiveEntryStatus.pending ? 'admin.emptyPendingDescription' : 'admin.emptyDescription')}
        />
      ) : (
        <ul className="flex flex-col divide-y rounded-xl border">
          {page.items.map((entry) => {
            const cover = entry.cover ? (entry.cover.kind === MediaKind.video ? entry.cover.posterUrl : entry.cover.url) : null
            return (
              <li key={entry.id}>
                <Link to={`/mural/${entry.id}`} className="flex gap-3 p-3 hover:bg-accent/50 focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none sm:p-4">
                  <span className="size-16 shrink-0 overflow-hidden rounded-lg bg-muted sm:size-20">
                    {cover ? (
                      <img src={cover} alt="" loading="lazy" className="size-full object-cover" />
                    ) : (
                      <span className="flex size-full items-center justify-center text-muted-foreground">
                        <CassetteTapeIcon className="size-6" />
                      </span>
                    )}
                  </span>
                  <span className="flex min-w-0 flex-1 flex-col gap-1">
                    <span className="flex flex-wrap items-center gap-2">
                      <AppBadge tone={archiveStatusTone[entry.status]}>{t(`status.${archiveStatusKey[entry.status]}`)}</AppBadge>
                      <span className="app-meta">{formatArchiveDate(entry)}</span>
                    </span>
                    <span className="truncate font-medium">{entry.title}</span>
                    <span className="app-meta flex flex-wrap items-center gap-3">
                      <span className="flex items-center gap-1.5">
                        <AppAvatar name={entry.author.name} src={entry.author.avatarUrl} size="sm" />@{entry.author.nickname}
                      </span>
                      <span>{formatRelative(entry.createdAt)}</span>
                      {entry.photoCount > 0 ? (
                        <span className="flex items-center gap-1 tabular-nums">
                          <CameraIcon className="size-3.5" />
                          {entry.photoCount}
                        </span>
                      ) : null}
                      {entry.videoCount > 0 ? (
                        <span className="flex items-center gap-1 tabular-nums">
                          <VideoIcon className="size-3.5" />
                          {entry.videoCount}
                        </span>
                      ) : null}
                    </span>
                  </span>
                </Link>
              </li>
            )
          })}
        </ul>
      )}

      <AppPagination page={page.page} totalPages={page.totalPages} totalCount={page.totalCount} onChange={page.setPage} loading={page.loading} />
    </div>
  )
}
