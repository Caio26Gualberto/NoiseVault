import { CheckCheckIcon, Trash2Icon } from 'lucide-react'
import { Trans, useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { AppAvatar } from '@/components/app-avatar'
import { AppBadge } from '@/components/app-badge'
import type { AppBadgeTone } from '@/components/app-badge'
import { AppIconButton } from '@/components/app-button'
import { formatRelative } from '@/lib/format'
import { cn } from '@/lib/utils'
import { BandInvitationStatus } from '@/pages/bandas/band-invitation-status'

import { notificationKinds, notificationMessage } from '../notification-message'
import type { NotificationDto } from '../services/notificacoes-service'

type InvitationState = 'pending' | 'accepted' | 'declined' | 'revoked' | 'expired'

const invitationTone: Record<InvitationState, AppBadgeTone> = {
  pending: 'brand',
  accepted: 'success',
  declined: 'neutral',
  revoked: 'neutral',
  expired: 'neutral',
}

const invitationStates: Record<BandInvitationStatus, InvitationState> = {
  [BandInvitationStatus.pending]: 'pending',
  [BandInvitationStatus.accepted]: 'accepted',
  [BandInvitationStatus.declined]: 'declined',
  [BandInvitationStatus.revoked]: 'revoked',
}

function invitationState(invitation: NonNullable<NotificationDto['invitation']>): InvitationState {
  const state = invitationStates[invitation.status]
  return state === 'pending' && new Date(invitation.expiresAt).getTime() < Date.now() ? 'expired' : state
}

type NotificationItemProps = {
  item: NotificationDto
  onOpen: (item: NotificationDto) => void
  onMarkRead: (item: NotificationDto) => void
  onRemove: (item: NotificationDto) => void
}

export function NotificationItem({ item, onOpen, onMarkRead, onRemove }: NotificationItemProps) {
  const { t } = useTranslation('notificacoes')
  const config = notificationKinds[item.kind]
  const KindIcon = config.icon
  const link = config.link(item)
  const isUnread = item.readAt === null
  const { key, values } = notificationMessage(item)
  const avatarName = item.actor?.name ?? item.band?.name ?? null
  const avatarSrc = item.actor?.avatarUrl ?? item.band?.avatarUrl ?? null
  const invitation = item.invitation ? invitationState(item.invitation) : null

  const body = (
    <>
      <span className="relative shrink-0">
        {avatarName ? (
          <AppAvatar name={avatarName} src={avatarSrc} size="lg" />
        ) : (
          <span className="flex size-10 items-center justify-center rounded-full bg-muted text-muted-foreground">
            <KindIcon className="size-5" />
          </span>
        )}
        {avatarName ? (
          <span className="absolute -right-1 -bottom-1 flex size-5 items-center justify-center rounded-full border-2 border-card bg-primary text-primary-foreground">
            <KindIcon className="size-3" />
          </span>
        ) : null}
      </span>
      <span className="flex min-w-0 flex-1 flex-col gap-1">
        <span className={cn('text-sm text-muted-foreground [&_b]:text-foreground', isUnread ? '[&_b]:font-semibold' : '[&_b]:font-medium')}>
          <Trans t={t} i18nKey={key as never} values={values} components={{ b: <b /> }} />
        </span>
        {item.post?.excerpt ? <span className="line-clamp-2 text-sm text-muted-foreground italic">“{item.post.excerpt}”</span> : null}
        {invitation ? (
          <AppBadge tone={invitationTone[invitation]} className="self-start">
            {t(`invitationStatus.${invitation}`)}
          </AppBadge>
        ) : null}
        <span className="app-meta">{formatRelative(item.lastActivityAt)}</span>
      </span>
    </>
  )

  return (
    <li className={cn('flex gap-2 p-3 sm:p-4', isUnread && 'bg-primary/[0.04]')}>
      <span className="mt-4 flex w-2 shrink-0 justify-center">
        {isUnread ? <span className="size-2 rounded-full bg-primary" aria-label={t('unread')} /> : null}
      </span>
      {link ? (
        <Link
          to={link}
          onClick={() => onOpen(item)}
          className="-m-1 flex min-w-0 flex-1 gap-3 rounded-lg p-1 focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
        >
          {body}
        </Link>
      ) : (
        <div className="flex min-w-0 flex-1 gap-3">{body}</div>
      )}
      <div className="flex shrink-0 flex-col gap-1">
        {isUnread ? (
          <AppIconButton label={t('markRead')} size="sm" onClick={() => onMarkRead(item)}>
            <CheckCheckIcon />
          </AppIconButton>
        ) : null}
        <AppIconButton label={t('remove.action')} size="sm" onClick={() => onRemove(item)}>
          <Trash2Icon />
        </AppIconButton>
      </div>
    </li>
  )
}
