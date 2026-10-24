import { CheckCheckIcon, CheckIcon, Trash2Icon } from 'lucide-react'
import { useState } from 'react'
import { Trans, useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { AppAvatar } from '@/components/app-avatar'
import { AppBadge } from '@/components/app-badge'
import type { AppBadgeTone } from '@/components/app-badge'
import { AppButton, AppIconButton } from '@/components/app-button'
import { isExternalUrl } from '@/ensaio/frame'
import { formatRelative } from '@/lib/format'
import { cn } from '@/lib/utils'
import { bandPagePath } from '@/pages/bandas/band-kind'
import { BandInvitationStatus } from '@/pages/bandas/band-invitation-status'
import { BandPartnershipStatus } from '@/pages/bandas/band-partnership-status'
import { acceptBandPartnership, declineBandPartnership } from '@/pages/bandas/services/bandas-service'

import { NotificationKind } from '../notification-kind'

import { notificationKinds, notificationMessage, notificationPlainText } from '../notification-message'
import type { NotificationDto } from '../services/notificacoes-service'

const targetClass = 'absolute inset-0 focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none focus-visible:ring-inset'

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

const partnershipStates: Record<BandPartnershipStatus, InvitationState> = {
  [BandPartnershipStatus.pending]: 'pending',
  [BandPartnershipStatus.accepted]: 'accepted',
  [BandPartnershipStatus.declined]: 'declined',
  [BandPartnershipStatus.revoked]: 'revoked',
}

function partnershipState(partnership: NonNullable<NotificationDto['partnership']>): InvitationState {
  const state = partnershipStates[partnership.status]
  return state === 'pending' && new Date(partnership.expiresAt).getTime() < Date.now() ? 'expired' : state
}

type NotificationItemProps = {
  item: NotificationDto
  onOpen: (item: NotificationDto) => void
  onMarkRead: (item: NotificationDto) => void
  onRemove: (item: NotificationDto) => void
  onChanged?: () => void
}

export function NotificationItem({ item, onOpen, onMarkRead, onRemove, onChanged }: NotificationItemProps) {
  const { t } = useTranslation('notificacoes')
  const [answering, setAnswering] = useState(false)
  const config = notificationKinds[item.kind]
  const KindIcon = config.icon
  const link = config.link(item)
  const isUnread = item.readAt === null
  const { key, values } = notificationMessage(item)
  const plainText = notificationPlainText(item, t)
  const bandFirst = item.kind === NotificationKind.followedBandPosted
  const avatarIsBand = bandFirst ? Boolean(item.band) : !item.actor && Boolean(item.band)
  const avatarName = (avatarIsBand ? item.band?.name : item.actor?.name) ?? null
  const avatarSrc = (avatarIsBand ? item.band?.avatarUrl : item.actor?.avatarUrl) ?? null
  const profileLink = avatarIsBand && item.band ? bandPagePath(item.band) : item.actor ? `/perfil/${item.actor.nickname}` : null
  const invitation = item.invitation ? invitationState(item.invitation) : item.partnership ? partnershipState(item.partnership) : null
  const canAnswerPartnership = item.kind === NotificationKind.bandPartnershipInvited && invitation === 'pending'

  const answerPartnership = async (accept: boolean) => {
    if (!item.partnership) return
    setAnswering(true)
    try {
      await (accept ? acceptBandPartnership(item.partnership.id) : declineBandPartnership(item.partnership.id))
      onChanged?.()
    } catch {
      return
    } finally {
      setAnswering(false)
    }
  }
  const nameClass = cn('text-foreground', isUnread ? 'font-semibold' : 'font-medium')

  const avatar = avatarName ? (
    <>
      <AppAvatar name={avatarName} src={avatarSrc} size="lg" />
      <span className="absolute -right-1 -bottom-1 flex size-5 items-center justify-center rounded-full border-2 border-card bg-primary text-primary-foreground">
        <KindIcon className="size-3" />
      </span>
    </>
  ) : (
    <span className="flex size-10 items-center justify-center rounded-full bg-muted text-muted-foreground">
      <KindIcon className="size-5" />
    </span>
  )

  const body = (
    <>
      {profileLink && avatarName ? (
        <Link to={profileLink} onClick={() => onOpen(item)} aria-label={avatarName} className="relative z-10 shrink-0 self-start rounded-full focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none">
          {avatar}
        </Link>
      ) : (
        <span className="relative shrink-0">{avatar}</span>
      )}
      <span className="flex min-w-0 flex-1 flex-col gap-1">
        <span className={cn('text-sm text-muted-foreground [&_b]:text-foreground', isUnread ? '[&_b]:font-semibold' : '[&_b]:font-medium')}>
          <Trans
            t={t}
            i18nKey={key as never}
            values={values}
            components={{
              b: <b />,
              actor: profileLink ? <Link to={profileLink} onClick={() => onOpen(item)} className={cn('relative z-10 hover:underline', nameClass)} /> : <b />,
            }}
          />
        </span>
        {item.post?.excerpt ? <span className="line-clamp-2 text-sm text-muted-foreground italic">“{item.post.excerpt}”</span> : null}
        {canAnswerPartnership ? (
          <span className="relative z-10 flex gap-2">
            <AppButton size="sm" loading={answering} onClick={() => void answerPartnership(true)}>
              <CheckIcon />
              {t('partnership.accept')}
            </AppButton>
            <AppButton size="sm" intent="ghost" disabled={answering} onClick={() => void answerPartnership(false)}>
              {t('partnership.decline')}
            </AppButton>
          </span>
        ) : invitation ? (
          <AppBadge tone={invitationTone[invitation]} className="self-start">
            {t(`invitationStatus.`)}
          </AppBadge>
        ) : null}
        <span className="app-meta">{formatRelative(item.lastActivityAt)}</span>
      </span>
    </>
  )

  return (
    <li className={cn('relative flex gap-2 p-3 sm:p-4', isUnread && 'bg-primary/[0.04]')}>
      {link && isExternalUrl(link) ? (
        <a href={link} onClick={() => onOpen(item)} aria-label={plainText} className={targetClass} />
      ) : link ? (
        <Link to={link} onClick={() => onOpen(item)} aria-label={plainText} className={targetClass} />
      ) : null}
      <span className="mt-4 flex w-2 shrink-0 justify-center">
        {isUnread ? <span className="size-2 rounded-full bg-primary" aria-label={t('unread')} /> : null}
      </span>
      <div className="flex min-w-0 flex-1 gap-3">{body}</div>
      <div className="relative z-10 flex shrink-0 flex-col gap-1">
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
