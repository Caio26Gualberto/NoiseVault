import {
  CalendarCheckIcon,
  CalendarClockIcon,
  CalendarXIcon,
  CassetteTapeIcon,
  HeadsetIcon,
  HeartIcon,
  LifeBuoyIcon,
  MessageCircleIcon,
  MicVocalIcon,
  UserMinusIcon,
  UserPlusIcon,
  UsersIcon,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import type { TFunction } from 'i18next'

import { SupportTicketStatus } from '@/pages/reportar-problema/support-enums'
import { adminTicketPath, myTicketPath } from '@/pages/reportar-problema/support-paths'

import { NotificationKind } from './notification-kind'
import type { NotificationDto } from './services/notificacoes-service'

type KindConfig = {
  key: string
  icon: LucideIcon
  link: (n: NotificationDto) => string | null
}

const postLink = (n: NotificationDto) => (n.post ? `/feed/${n.post.id}` : null)
const eventLink = (n: NotificationDto) => (n.event ? `/eventos/${n.event.id}` : null)
const archiveLink = (n: NotificationDto) => (n.archive ? `/mural/${n.archive.id}` : null)
const bandLink = (n: NotificationDto) => (n.band ? `/bandas/${n.band.slug}` : null)
const ticketLink = (n: NotificationDto) => (n.ticket ? (n.ticket.mine ? myTicketPath(n.ticket.id) : adminTicketPath(n.ticket.id)) : null)

export const notificationKinds: Record<NotificationKind, KindConfig> = {
  [NotificationKind.postLiked]: { key: 'postLiked', icon: HeartIcon, link: postLink },
  [NotificationKind.postCommented]: { key: 'postCommented', icon: MessageCircleIcon, link: postLink },
  [NotificationKind.eventCommented]: { key: 'eventCommented', icon: MessageCircleIcon, link: eventLink },
  [NotificationKind.eventInterest]: { key: 'eventInterest', icon: CalendarCheckIcon, link: eventLink },
  [NotificationKind.eventCancelled]: { key: 'eventCancelled', icon: CalendarXIcon, link: eventLink },
  [NotificationKind.eventTomorrow]: { key: 'eventTomorrow', icon: CalendarClockIcon, link: eventLink },
  [NotificationKind.eventLineup]: { key: 'eventLineup', icon: MicVocalIcon, link: eventLink },
  [NotificationKind.archiveLiked]: { key: 'archiveLiked', icon: HeartIcon, link: archiveLink },
  [NotificationKind.archiveCommented]: { key: 'archiveCommented', icon: MessageCircleIcon, link: archiveLink },
  [NotificationKind.archiveAttended]: { key: 'archiveAttended', icon: CassetteTapeIcon, link: archiveLink },
  [NotificationKind.archiveCommentedWhereAttended]: { key: 'archiveCommentedWhereAttended', icon: CassetteTapeIcon, link: archiveLink },
  [NotificationKind.bandInvite]: { key: 'bandInvite', icon: UserPlusIcon, link: () => null },
  [NotificationKind.bandInviteAccepted]: { key: 'bandInviteAccepted', icon: UsersIcon, link: bandLink },
  [NotificationKind.bandInviteDeclined]: { key: 'bandInviteDeclined', icon: UserMinusIcon, link: bandLink },
  [NotificationKind.bandPromoted]: { key: 'bandPromoted', icon: UsersIcon, link: bandLink },
  [NotificationKind.bandRemoved]: { key: 'bandRemoved', icon: UserMinusIcon, link: bandLink },
  [NotificationKind.bandMemberLeft]: { key: 'bandMemberLeft', icon: UserMinusIcon, link: bandLink },
  [NotificationKind.supportTicketCreated]: { key: 'supportTicketCreated', icon: LifeBuoyIcon, link: ticketLink },
  [NotificationKind.supportTicketReplied]: { key: 'supportTicketReplied', icon: HeadsetIcon, link: ticketLink },
  [NotificationKind.supportTicketStatusChanged]: { key: 'supportTicketStatusChanged', icon: LifeBuoyIcon, link: ticketLink },
}

const ticketStatusKey: Record<SupportTicketStatus, string> = {
  [SupportTicketStatus.open]: 'supportTicketReopened',
  [SupportTicketStatus.inProgress]: 'supportTicketInProgress',
  [SupportTicketStatus.resolved]: 'supportTicketResolved',
  [SupportTicketStatus.dismissed]: 'supportTicketDismissed',
}

function messageKey(n: NotificationDto) {
  if (n.kind === NotificationKind.bandInvite && n.invitation?.instrument) return 'bandInviteInstrument'
  if (n.kind === NotificationKind.supportTicketStatusChanged && n.ticket) return ticketStatusKey[n.ticket.status]
  if (n.kind === NotificationKind.supportTicketReplied && n.ticket?.mine) return 'supportTicketRepliedToYou'
  return notificationKinds[n.kind].key
}

export function notificationMessage(n: NotificationDto) {
  const instrument = n.invitation?.instrument ?? null

  return {
    key: `kinds.${messageKey(n)}`,
    values: {
      actor: n.actor?.name ?? '',
      count: Math.max(n.actorCount - 1, 0),
      event: n.event?.title ?? '',
      archive: n.archive?.title ?? '',
      band: n.band?.name ?? '',
      instrument: instrument ?? '',
      protocol: n.ticket?.protocol ?? '',
      ticket: n.ticket?.title ?? '',
    },
  }
}

export function notificationPlainText(n: NotificationDto, t: TFunction<'notificacoes'>) {
  const { key, values } = notificationMessage(n)
  return String(t(key as never, values)).replace(/<\/?b>/g, '')
}
