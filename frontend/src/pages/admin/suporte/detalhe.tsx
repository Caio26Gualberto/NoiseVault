import { ArrowLeftIcon, InboxIcon, Trash2Icon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router-dom'

import { AppAvatar } from '@/components/app-avatar'
import { AppBadge } from '@/components/app-badge'
import { AppButton } from '@/components/app-button'
import { AppCard } from '@/components/app-card'
import { useAppConfirm } from '@/components/app-confirm'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppPageHeader } from '@/components/app-page-header'
import { AppSkeleton } from '@/components/app-skeleton'
import { supportPriorityTone, useSupportLabels } from '@/pages/ajuda/support-enums'
import { adminSupportPath } from '@/pages/ajuda/support-paths'
import { TicketConversation } from '@/pages/suporte/components/ticket-conversation'
import { TicketStatusBadge, TicketSummary } from '@/pages/suporte/components/ticket-summary'
import { useTicketDetail } from '@/pages/suporte/hooks/use-ticket-detail'
import { useTicketMessages } from '@/pages/suporte/hooks/use-ticket-messages'

import { TriagePanel } from './components/triage-panel'
import { deleteTicket, getMessages, getTicket, replyAsStaff } from './services/suporte-admin-service'
import type { SupportTicketAdminDto } from './services/suporte-admin-service'

function ContextCard({ ticket }: { ticket: SupportTicketAdminDto }) {
  const { t } = useTranslation('suporte')
  const viewport = ticket.viewportWidth && ticket.viewportHeight ? `${ticket.viewportWidth} × ${ticket.viewportHeight}` : null

  return (
    <AppCard title={t('admin.context.title')}>
      <dl className="flex flex-col gap-3 text-sm">
        <div className="flex flex-col gap-1">
          <dt className="app-meta">{t('admin.context.reporter')}</dt>
          <dd>
            <Link to={`/perfil/${ticket.reporter.nickname}`} className="flex items-center gap-2 hover:underline">
              <AppAvatar name={ticket.reporter.name} src={ticket.reporter.avatarUrl} size="sm" />
              <span>
                {ticket.reporter.name} <span className="text-muted-foreground">@{ticket.reporter.nickname}</span>
              </span>
            </Link>
          </dd>
        </div>
        <div className="flex flex-col gap-1">
          <dt className="app-meta">{t('admin.context.origin')}</dt>
          <dd>
            {ticket.originPath ? (
              <Link to={ticket.originPath} className="font-mono text-xs break-all text-primary hover:underline">
                {ticket.originPath}
              </Link>
            ) : (
              <span className="text-muted-foreground">{t('admin.context.unknown')}</span>
            )}
          </dd>
        </div>
        <div className="flex flex-col gap-1">
          <dt className="app-meta">{t('admin.context.viewport')}</dt>
          <dd className="tabular-nums">{viewport ?? <span className="text-muted-foreground">{t('admin.context.unknown')}</span>}</dd>
        </div>
        <div className="flex flex-col gap-1">
          <dt className="app-meta">{t('admin.context.userAgent')}</dt>
          <dd className="font-mono text-xs break-all text-muted-foreground">{ticket.userAgent ?? t('admin.context.unknown')}</dd>
        </div>
      </dl>
    </AppCard>
  )
}

export function SuporteAdminDetalhePage() {
  const { t } = useTranslation('suporte')
  const labels = useSupportLabels()
  const id = Number(useParams().id)
  const navigate = useNavigate()
  const confirm = useAppConfirm()
  const { ticket, loading, refresh, replace } = useTicketDetail(id, getTicket)

  const removeTicket = () =>
    confirm({
      title: t('admin.deleteTitle'),
      message: t('admin.deleteMessage'),
      confirmLabel: t('admin.delete'),
      danger: true,
      onConfirm: async () => {
        await deleteTicket(id)
        navigate(adminSupportPath, { replace: true })
      },
    })
  const messages = useTicketMessages(id, getMessages, async (ticketId, text) => {
    const message = await replyAsStaff(ticketId, text)
    refresh()
    return message
  })

  const back = (
    <AppButton intent="ghost" asChild>
      <Link to={adminSupportPath}>
        <ArrowLeftIcon />
        {t('admin.title')}
      </Link>
    </AppButton>
  )

  if (loading && !ticket) return <AppSkeleton preset="page" />

  if (!ticket)
    return (
      <div className="app-page-enter flex flex-col gap-4">
        {back}
        <AppEmptyState icon={InboxIcon} title={t('detail.notFound')} />
      </div>
    )

  return (
    <div className="app-page-enter flex flex-col">
      <div className="mb-2">{back}</div>
      <AppPageHeader
        title={ticket.title}
        actions={
          ticket.canDelete ? (
            <AppButton type="button" intent="danger" onClick={removeTicket}>
              <Trash2Icon />
              {t('admin.delete')}
            </AppButton>
          ) : null
        }
        meta={
          <span className="flex flex-wrap items-center gap-2">
            <span className="tabular-nums">{ticket.protocol}</span>
            <TicketStatusBadge ticket={ticket} />
            <AppBadge tone={supportPriorityTone[ticket.priority]}>{labels.priority(ticket.priority)}</AppBadge>
          </span>
        }
      />
      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_20rem]">
        <div className="flex min-w-0 flex-col gap-6">
          <AppCard>
            <TicketSummary ticket={ticket} />
          </AppCard>
          <TicketConversation
            messages={messages}
            canSend={ticket.canReply}
            placeholder={t('conversation.staffPlaceholder')}
            emptyText={t('conversation.emptyStaff')}
            hint={t('conversation.staffHint')}
          />
        </div>
        <div className="flex flex-col gap-6">
          {ticket.canEdit ? (
            <TriagePanel key={`${ticket.status}-${ticket.priority}-${ticket.internalNote ?? ''}`} ticket={ticket} onSaved={replace} />
          ) : null}
          <ContextCard ticket={ticket} />
        </div>
      </div>
    </div>
  )
}
