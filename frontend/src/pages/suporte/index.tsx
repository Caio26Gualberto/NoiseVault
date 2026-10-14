import { InboxIcon, MessageSquareIcon, PlusIcon } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { AppBadge } from '@/components/app-badge'
import { AppButton } from '@/components/app-button'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppPageHeader } from '@/components/app-page-header'
import { AppPagination, defaultPageSize } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { reportProblemPath } from '@/layout/last-location'
import { formatRelative } from '@/lib/format'
import { getMyTickets } from '@/pages/reportar-problema/services/suporte-service'
import type { SupportTicketDto } from '@/pages/reportar-problema/services/suporte-service'
import { isSupportTicketOpen } from '@/pages/reportar-problema/support-enums'
import { myTicketPath } from '@/pages/reportar-problema/support-paths'

import { TicketStatusBadge } from './components/ticket-summary'

export function SuportePage() {
  const { t } = useTranslation('suporte')
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<SupportTicketDto>>(() => emptyPage(defaultPageSize))
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    getMyTickets({ page, pageSize: defaultPageSize })
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
  }, [page])

  return (
    <div className="app-page-enter mx-auto flex max-w-2xl flex-col">
      <AppPageHeader
        title={t('mine.title')}
        description={t('mine.description')}
        actions={
          <AppButton asChild>
            <Link to={reportProblemPath}>
              <PlusIcon />
              {t('mine.new')}
            </Link>
          </AppButton>
        }
      />

      {loading ? (
        <AppSkeleton preset="list" count={4} />
      ) : result.items.length === 0 ? (
        <AppEmptyState icon={InboxIcon} title={t('mine.emptyTitle')} description={t('mine.emptyDescription')} />
      ) : (
        <ul className="flex flex-col divide-y rounded-xl border">
          {result.items.map((ticket) => {
            const answered = isSupportTicketOpen(ticket.status) && !ticket.awaitingStaff && ticket.messageCount > 0
            return (
              <li key={ticket.id}>
                <Link
                  to={myTicketPath(ticket.id)}
                  className="flex flex-col gap-1.5 p-4 hover:bg-accent/50 focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
                >
                  <span className="flex flex-wrap items-center gap-2">
                    <span className="app-meta tabular-nums">{ticket.protocol}</span>
                    <TicketStatusBadge ticket={ticket} />
                    {answered ? <AppBadge tone="highlight">{t('mine.answered')}</AppBadge> : null}
                  </span>
                  <span className="font-medium">{ticket.title}</span>
                  <span className="app-meta flex items-center gap-3">
                    <span>{formatRelative(ticket.lastActivityAt)}</span>
                    {ticket.messageCount > 0 ? (
                      <span className="flex items-center gap-1 tabular-nums">
                        <MessageSquareIcon className="size-3.5" />
                        {ticket.messageCount}
                      </span>
                    ) : null}
                  </span>
                </Link>
              </li>
            )
          })}
        </ul>
      )}

      <AppPagination page={result.page} totalPages={result.totalPages} totalCount={result.totalCount} onChange={setPage} loading={loading} />
    </div>
  )
}
