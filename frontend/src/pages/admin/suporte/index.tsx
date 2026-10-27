import { ImageIcon, InboxIcon, MessageSquareIcon, SearchIcon, VideoIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { AppAvatar } from '@/components/app-avatar'
import { AppBadge } from '@/components/app-badge'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppPageHeader } from '@/components/app-page-header'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { formatRelative } from '@/lib/format'
import { cn } from '@/lib/utils'
import {
  SupportTicketPriority,
  SupportTicketStatus,
  allSupportAreas,
  allSupportPriorities,
  allSupportStatuses,
  allSupportTags,
  isSupportTicketOpen,
  supportPriorityDot,
  supportPriorityTone,
  useSupportLabels,
} from '@/pages/ajuda/support-enums'
import type { SupportArea, SupportTag } from '@/pages/ajuda/support-enums'
import { adminTicketPath } from '@/pages/ajuda/support-paths'
import { TicketStatusBadge } from '@/pages/suporte/components/ticket-summary'

import { useSuporteAdminPage } from './hooks/use-suporte-admin-page'
import type { StatusTab } from './hooks/use-suporte-admin-page'
import type { SupportSort, SupportSummaryDto } from './services/suporte-admin-service'

const all = 'all'

const summaryCount: Record<SupportTicketStatus, (summary: SupportSummaryDto) => number> = {
  [SupportTicketStatus.open]: (s) => s.open,
  [SupportTicketStatus.inProgress]: (s) => s.inProgress,
  [SupportTicketStatus.resolved]: (s) => s.resolved,
  [SupportTicketStatus.dismissed]: (s) => s.dismissed,
}


export function SuporteAdminPage() {
  const { t } = useTranslation('suporte')
  const labels = useSupportLabels()
  const page = useSuporteAdminPage()

  return (
    <div className="app-page-enter flex flex-col">
      <AppPageHeader title={t('admin.title')} description={t('admin.description')} />

      <Tabs value={String(page.status)} onValueChange={(value) => page.changeStatus(value === all ? all : (Number(value) as StatusTab))} className="mb-4 max-w-full overflow-x-auto overflow-y-hidden [scrollbar-width:none]">
        <TabsList className="w-max">
          {allSupportStatuses.map((status) => (
            <TabsTrigger key={status} value={String(status)} className="gap-1.5">
              {labels.status(status)}
              {page.summary ? <span className="text-muted-foreground tabular-nums">{summaryCount[status](page.summary)}</span> : null}
            </TabsTrigger>
          ))}
          <TabsTrigger value={all}>{t('admin.all')}</TabsTrigger>
        </TabsList>
      </Tabs>

      <div className="mb-6 flex flex-col gap-3 lg:flex-row">
        <div className="relative flex-1">
          <SearchIcon className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
          <input
            type="search"
            value={page.search}
            onChange={(event) => page.changeSearch(event.target.value)}
            placeholder={t('admin.searchPlaceholder')}
            aria-label={t('admin.searchPlaceholder')}
            className="h-10 w-full rounded-lg border bg-transparent pl-9 pr-3 text-sm placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
          />
        </div>
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4 lg:flex">
          <Select value={page.tag === null ? all : String(page.tag)} onValueChange={(value) => page.changeTag(value === all ? null : (Number(value) as SupportTag))}>
            <SelectTrigger className="h-10 lg:w-40" aria-label={t('admin.filters.tag')}>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={all}>{t('admin.filters.allTags')}</SelectItem>
              {allSupportTags.map((tag) => (
                <SelectItem key={tag} value={String(tag)}>
                  {labels.tag(tag)}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Select value={page.area === null ? all : String(page.area)} onValueChange={(value) => page.changeArea(value === all ? null : (Number(value) as SupportArea))}>
            <SelectTrigger className="h-10 lg:w-44" aria-label={t('admin.filters.area')}>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={all}>{t('admin.filters.allAreas')}</SelectItem>
              {allSupportAreas.map((area) => (
                <SelectItem key={area} value={String(area)}>
                  {labels.area(area)}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Select
            value={page.priority === null ? all : String(page.priority)}
            onValueChange={(value) => page.changePriority(value === all ? null : (Number(value) as SupportTicketPriority))}
          >
            <SelectTrigger className="h-10 lg:w-40" aria-label={t('admin.filters.priority')}>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={all}>{t('admin.filters.allPriorities')}</SelectItem>
              {allSupportPriorities.map((priority) => (
                <SelectItem key={priority} value={String(priority)}>
                  {labels.priority(priority)}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Select value={page.sort} onValueChange={(value) => page.changeSort(value as SupportSort)}>
            <SelectTrigger className="h-10 lg:w-44" aria-label={t('admin.filters.sort')}>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="activity">{t('admin.sort.activity')}</SelectItem>
              <SelectItem value="newest">{t('admin.sort.newest')}</SelectItem>
              <SelectItem value="priority">{t('admin.sort.priority')}</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      {page.loading ? (
        <AppSkeleton preset="list" count={5} />
      ) : page.items.length === 0 ? (
        <AppEmptyState icon={InboxIcon} title={t('admin.emptyTitle')} description={t('admin.emptyDescription')} />
      ) : (
        <ul className="flex flex-col divide-y rounded-xl border">
          {page.items.map((ticket) => (
            <li key={ticket.id}>
              <Link
                to={adminTicketPath(ticket.id)}
                className="flex gap-3 p-4 hover:bg-accent/50 focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
              >
                <span className={cn('mt-1.5 size-2 shrink-0 rounded-full', supportPriorityDot[ticket.priority])} aria-label={labels.priority(ticket.priority)} />
                <span className="flex min-w-0 flex-1 flex-col gap-1.5">
                  <span className="flex flex-wrap items-center gap-2">
                    <span className="app-meta tabular-nums">{ticket.protocol}</span>
                    <TicketStatusBadge ticket={ticket} />
                    {ticket.priority === SupportTicketPriority.high ? (
                      <AppBadge tone={supportPriorityTone[ticket.priority]}>{labels.priority(ticket.priority)}</AppBadge>
                    ) : null}
                    {ticket.awaitingStaff && isSupportTicketOpen(ticket.status) ? <AppBadge tone="highlight">{t('admin.awaiting')}</AppBadge> : null}
                  </span>
                  <span className="truncate font-medium">{ticket.title}</span>
                  <span className="flex flex-wrap gap-1.5">
                    {ticket.tags.map((tag) => (
                      <AppBadge key={tag}>{labels.tag(tag)}</AppBadge>
                    ))}
                  </span>
                  <span className="app-meta flex flex-wrap items-center gap-3">
                    <span className="flex items-center gap-1.5">
                      <AppAvatar name={ticket.reporter.name} src={ticket.reporter.avatarUrl} size="sm" />@{ticket.reporter.nickname}
                    </span>
                    <span>{formatRelative(ticket.lastActivityAt)}</span>
                    {ticket.photoCount > 0 ? (
                      <span className="flex items-center gap-1 tabular-nums">
                        <ImageIcon className="size-3.5" />
                        {ticket.photoCount}
                      </span>
                    ) : null}
                    {ticket.videoCount > 0 ? (
                      <span className="flex items-center gap-1 tabular-nums">
                        <VideoIcon className="size-3.5" />
                        {ticket.videoCount}
                      </span>
                    ) : null}
                    {ticket.messageCount > 0 ? (
                      <span className="flex items-center gap-1 tabular-nums">
                        <MessageSquareIcon className="size-3.5" />
                        {ticket.messageCount}
                      </span>
                    ) : null}
                  </span>
                </span>
              </Link>
            </li>
          ))}
        </ul>
      )}

      <AppPagination page={page.page} totalPages={page.totalPages} totalCount={page.totalCount} onChange={page.setPage} loading={page.loading} />
    </div>
  )
}
