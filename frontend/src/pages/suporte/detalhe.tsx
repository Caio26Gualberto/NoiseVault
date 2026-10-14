import { ArrowLeftIcon, InboxIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'

import { AppButton } from '@/components/app-button'
import { AppCard } from '@/components/app-card'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppPageHeader } from '@/components/app-page-header'
import { AppSkeleton } from '@/components/app-skeleton'
import { getMyMessages, getMyTicket, replyToMyTicket } from '@/pages/reportar-problema/services/suporte-service'
import { SupportTicketStatus } from '@/pages/reportar-problema/support-enums'
import { myTicketsPath } from '@/pages/reportar-problema/support-paths'

import { TicketConversation } from './components/ticket-conversation'
import { TicketStatusBadge, TicketSummary } from './components/ticket-summary'
import { useTicketDetail } from './hooks/use-ticket-detail'
import { useTicketMessages } from './hooks/use-ticket-messages'

export function SuporteDetalhePage() {
  const { t } = useTranslation('suporte')
  const id = Number(useParams().id)
  const { ticket, loading, refresh } = useTicketDetail(id, getMyTicket)
  const messages = useTicketMessages(id, getMyMessages, async (ticketId, text) => {
    const message = await replyToMyTicket(ticketId, text)
    refresh()
    return message
  })

  const back = (
    <AppButton intent="ghost" asChild>
      <Link to={myTicketsPath}>
        <ArrowLeftIcon />
        {t('mine.title')}
      </Link>
    </AppButton>
  )

  if (loading && !ticket) return <AppSkeleton preset="page" />

  if (!ticket)
    return (
      <div className="app-page-enter mx-auto flex max-w-2xl flex-col gap-4">
        {back}
        <AppEmptyState icon={InboxIcon} title={t('detail.notFound')} />
      </div>
    )

  const dismissed = ticket.status === SupportTicketStatus.dismissed
  const resolved = ticket.status === SupportTicketStatus.resolved

  return (
    <div className="app-page-enter mx-auto flex max-w-2xl flex-col">
      <div className="mb-2">{back}</div>
      <AppPageHeader
        title={ticket.title}
        meta={
          <span className="flex items-center gap-2">
            <span className="tabular-nums">{ticket.protocol}</span>
            <TicketStatusBadge ticket={ticket} />
          </span>
        }
      />
      <div className="flex flex-col gap-6">
        <AppCard>
          <TicketSummary ticket={ticket} />
        </AppCard>
        <TicketConversation
          messages={messages}
          canSend={!dismissed}
          closedNotice={dismissed ? t('conversation.closed') : null}
          hint={resolved ? t('conversation.reopenHint') : null}
          placeholder={t('conversation.placeholder')}
          emptyText={t('conversation.emptyReporter')}
        />
      </div>
    </div>
  )
}
