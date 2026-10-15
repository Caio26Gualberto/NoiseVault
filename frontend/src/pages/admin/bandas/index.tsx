import { BadgeCheckIcon, SearchIcon } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { Permissions } from '@/auth/permissions'
import { useCan } from '@/auth/use-can'
import { AppAvatar } from '@/components/app-avatar'
import { AppBadge } from '@/components/app-badge'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppPageHeader } from '@/components/app-page-header'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { formatRelative } from '@/lib/format'
import { BandVerificationStatus, allBandVerificationStatuses, bandVerificationStatusKey, bandVerificationStatusTone } from '@/pages/bandas/band-verification-status'
import type { BandVerificationDto } from '@/pages/bandas/services/bandas-service'

import { VerificationReviewDialog } from './components/verification-review-dialog'
import { useBandasAdminPage } from './hooks/use-bandas-admin-page'
import type { BandVerificationSummaryDto } from './services/bandas-admin-service'

const summaryCount: Record<BandVerificationStatus, (summary: BandVerificationSummaryDto) => number> = {
  [BandVerificationStatus.pending]: (s) => s.pending,
  [BandVerificationStatus.approved]: (s) => s.approved,
  [BandVerificationStatus.rejected]: (s) => s.rejected,
}

export function BandasAdminPage() {
  const { t } = useTranslation('bandas')
  const can = useCan()
  const page = useBandasAdminPage()
  const [selected, setSelected] = useState<BandVerificationDto | null>(null)

  return (
    <div className="app-page-enter flex flex-col">
      <AppPageHeader title={t('admin.title')} description={t('admin.description')} />

      <Tabs value={String(page.status)} onValueChange={(value) => page.changeStatus(Number(value) as BandVerificationStatus)} className="mb-4 max-w-full overflow-x-auto overflow-y-hidden [scrollbar-width:none]">
        <TabsList className="w-max">
          {allBandVerificationStatuses.map((status) => (
            <TabsTrigger key={status} value={String(status)} className="gap-1.5">
              {t(`admin.tabs.${bandVerificationStatusKey[status]}`)}
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
          icon={BadgeCheckIcon}
          title={t(page.status === BandVerificationStatus.pending ? 'admin.emptyPendingTitle' : 'admin.emptyTitle')}
          description={t(page.status === BandVerificationStatus.pending ? 'admin.emptyPendingDescription' : 'admin.emptyDescription')}
        />
      ) : (
        <ul className="flex flex-col divide-y rounded-xl border">
          {page.items.map((request) => (
            <li key={request.id}>
              <button
                type="button"
                onClick={() => setSelected(request)}
                className="flex w-full gap-3 p-3 text-left hover:bg-accent/50 focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none sm:p-4"
              >
                <AppAvatar name={request.band.name} src={request.band.avatarUrl} size="lg" verified={request.band.isVerified} />
                <span className="flex min-w-0 flex-1 flex-col gap-1">
                  <span className="flex flex-wrap items-center gap-2">
                    <AppBadge tone={bandVerificationStatusTone[request.status]}>{t(`verification.status.${bandVerificationStatusKey[request.status]}`)}</AppBadge>
                    <span className="app-meta">{formatRelative(request.createdAt)}</span>
                  </span>
                  <span className="truncate font-medium">{request.band.name}</span>
                  <span className="line-clamp-2 text-sm text-muted-foreground">{request.message}</span>
                  <span className="app-meta flex items-center gap-1.5">
                    <AppAvatar name={request.requestedBy.name} src={request.requestedBy.avatarUrl} size="sm" />@{request.requestedBy.nickname} · {t('admin.memberCount', { count: request.bandMemberCount })}
                  </span>
                </span>
              </button>
            </li>
          ))}
        </ul>
      )}

      <AppPagination page={page.page} totalPages={page.totalPages} totalCount={page.totalCount} onChange={page.setPage} loading={page.loading} />

      <VerificationReviewDialog
        request={selected}
        canVerify={can(Permissions.bandsVerify)}
        onOpenChange={(open) => {
          if (!open) setSelected(null)
        }}
        onChanged={page.refresh}
      />
    </div>
  )
}
