import {
  ArchiveIcon,
  BadgeCheckIcon,
  BadgeXIcon,
  AtSignIcon,
  CalendarCheckIcon,
  CalendarClockIcon,
  CalendarXIcon,
  CassetteTapeIcon,
  HandshakeIcon,
  HeadsetIcon,
  HeartIcon,
  LifeBuoyIcon,
  MegaphoneIcon,
  MessageCircleIcon,
  MicVocalIcon,
  RssIcon,
  TagIcon,
  UserMinusIcon,
  UserPlusIcon,
  UsersIcon,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import type { TFunction } from 'i18next'

import { ensaioUrl } from '@/ensaio/frame'
import i18n from '@/i18n'
import { bandPagePath } from '@/pages/bandas/band-kind'
import { Instrument, instrumentKey } from '@/pages/ensaio/ensaio-status'
import { mySubmissionsPath } from '@/pages/mural/archive-status'
import { adminBandsPath } from '@/pages/bandas/band-verification-status'
import { SupportTicketStatus } from '@/pages/ajuda/support-enums'
import { adminTicketPath, myTicketPath } from '@/pages/ajuda/support-paths'

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
const bandLink = (n: NotificationDto) => (n.band ? bandPagePath(n.band) : null)
const ticketLink = (n: NotificationDto) => (n.ticket ? (n.ticket.mine ? myTicketPath(n.ticket.id) : adminTicketPath(n.ticket.id)) : null)
const targetLink = (n: NotificationDto) => postLink(n) ?? eventLink(n) ?? archiveLink(n)

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
  [NotificationKind.archiveSubmissionReceived]: { key: 'archiveSubmissionReceived', icon: ArchiveIcon, link: archiveLink },
  [NotificationKind.archiveSubmissionApproved]: { key: 'archiveSubmissionApproved', icon: CassetteTapeIcon, link: archiveLink },
  [NotificationKind.archiveSubmissionRejected]: { key: 'archiveSubmissionRejected', icon: ArchiveIcon, link: () => mySubmissionsPath },
  [NotificationKind.mentionedInPost]: { key: 'mentionedInPost', icon: AtSignIcon, link: targetLink },
  [NotificationKind.mentionedInComment]: { key: 'mentionedInComment', icon: AtSignIcon, link: targetLink },
  [NotificationKind.bandMentionedInPost]: { key: 'bandMentionedInPost', icon: AtSignIcon, link: targetLink },
  [NotificationKind.bandMentionedInComment]: { key: 'bandMentionedInComment', icon: AtSignIcon, link: targetLink },
  [NotificationKind.photoTagged]: { key: 'photoTagged', icon: TagIcon, link: targetLink },
  [NotificationKind.bandOpeningInterest]: { key: 'bandOpeningInterest', icon: MegaphoneIcon, link: (n) => (n.opening ? ensaioUrl(`/vagas/${n.opening.id}`) : null) },
  [NotificationKind.bandVerificationRequested]: { key: 'bandVerificationRequested', icon: BadgeCheckIcon, link: () => adminBandsPath },
  [NotificationKind.bandVerificationApproved]: { key: 'bandVerificationApproved', icon: BadgeCheckIcon, link: bandLink },
  [NotificationKind.bandVerificationRejected]: { key: 'bandVerificationRejected', icon: BadgeXIcon, link: bandLink },
  [NotificationKind.bandVerificationRevoked]: { key: 'bandVerificationRevoked', icon: BadgeXIcon, link: bandLink },
  [NotificationKind.followedBandPosted]: { key: 'followedBandPosted', icon: RssIcon, link: postLink },
  [NotificationKind.bandPartnershipInvited]: { key: 'bandPartnershipInvited', icon: HandshakeIcon, link: bandLink },
  [NotificationKind.bandPartnershipAccepted]: { key: 'bandPartnershipAccepted', icon: HandshakeIcon, link: bandLink },
}

const withPlace = new Set<NotificationKind>([NotificationKind.mentionedInComment, NotificationKind.bandMentionedInComment, NotificationKind.photoTagged])

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
  if (withPlace.has(n.kind) && n.event) return `${notificationKinds[n.kind].key}Event`
  if (withPlace.has(n.kind) && n.archive) return `${notificationKinds[n.kind].key}Archive`
  return notificationKinds[n.kind].key
}

function openingInstrument(opening: { instrument: Instrument; otherName: string | null }) {
  if (opening.instrument === Instrument.other && opening.otherName) return opening.otherName
  return i18n.t(`ensaio:instruments.${instrumentKey[opening.instrument]}`).toLowerCase()
}

export function notificationMessage(n: NotificationDto) {
  const instrument = n.invitation?.instrument ?? (n.opening ? openingInstrument(n.opening) : null)

  return {
    key: `kinds.${messageKey(n)}`,
    values: {
      actor: n.actor?.name ?? '',
      count: Math.max(n.actorCount - 1, 0),
      event: n.event?.title ?? '',
      archive: n.archive?.title ?? '',
      band: n.band?.name ?? '',
      myBand: n.partnership?.myBand.name ?? '',
      instrument: instrument ?? '',
      protocol: n.ticket?.protocol ?? '',
      ticket: n.ticket?.title ?? '',
    },
  }
}

export function notificationPlainText(n: NotificationDto, t: TFunction<'notificacoes'>) {
  const { key, values } = notificationMessage(n)
  return String(t(key as never, values)).replace(/<\/?(b|actor)>/g, '')
}
