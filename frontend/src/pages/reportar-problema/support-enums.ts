import { useTranslation } from 'react-i18next'

import type { AppBadgeTone } from '@/components/app-badge'

export const SupportTag = { bug: 0, suggestion: 1, account: 2, mobile: 3, desktop: 4 } as const
export type SupportTag = (typeof SupportTag)[keyof typeof SupportTag]

export const SupportArea = {
  feed: 0,
  events: 1,
  archive: 2,
  profile: 3,
  bands: 4,
  notifications: 5,
  account: 6,
  signIn: 7,
  other: 8,
} as const
export type SupportArea = (typeof SupportArea)[keyof typeof SupportArea]

export const SupportTicketStatus = { open: 0, inProgress: 1, resolved: 2, dismissed: 3 } as const
export type SupportTicketStatus = (typeof SupportTicketStatus)[keyof typeof SupportTicketStatus]

export const SupportTicketPriority = { low: 0, normal: 1, high: 2 } as const
export type SupportTicketPriority = (typeof SupportTicketPriority)[keyof typeof SupportTicketPriority]

const tagKey: Record<SupportTag, string> = {
  [SupportTag.bug]: 'bug',
  [SupportTag.suggestion]: 'suggestion',
  [SupportTag.account]: 'account',
  [SupportTag.mobile]: 'mobile',
  [SupportTag.desktop]: 'desktop',
}

const areaKey: Record<SupportArea, string> = {
  [SupportArea.feed]: 'feed',
  [SupportArea.events]: 'events',
  [SupportArea.archive]: 'archive',
  [SupportArea.profile]: 'profile',
  [SupportArea.bands]: 'bands',
  [SupportArea.notifications]: 'notifications',
  [SupportArea.account]: 'account',
  [SupportArea.signIn]: 'signIn',
  [SupportArea.other]: 'other',
}

const statusKey: Record<SupportTicketStatus, string> = {
  [SupportTicketStatus.open]: 'open',
  [SupportTicketStatus.inProgress]: 'inProgress',
  [SupportTicketStatus.resolved]: 'resolved',
  [SupportTicketStatus.dismissed]: 'dismissed',
}

export const supportStatusTone: Record<SupportTicketStatus, AppBadgeTone> = {
  [SupportTicketStatus.open]: 'info',
  [SupportTicketStatus.inProgress]: 'warning',
  [SupportTicketStatus.resolved]: 'success',
  [SupportTicketStatus.dismissed]: 'neutral',
}

const priorityKey: Record<SupportTicketPriority, string> = {
  [SupportTicketPriority.low]: 'low',
  [SupportTicketPriority.normal]: 'normal',
  [SupportTicketPriority.high]: 'high',
}

export const supportPriorityTone: Record<SupportTicketPriority, AppBadgeTone> = {
  [SupportTicketPriority.low]: 'neutral',
  [SupportTicketPriority.normal]: 'neutral',
  [SupportTicketPriority.high]: 'danger',
}

export const supportPriorityDot: Record<SupportTicketPriority, string> = {
  [SupportTicketPriority.low]: 'bg-border',
  [SupportTicketPriority.normal]: 'bg-muted-foreground/60',
  [SupportTicketPriority.high]: 'bg-destructive',
}

export const allSupportTags = Object.values(SupportTag) as SupportTag[]
export const allSupportAreas = Object.values(SupportArea) as SupportArea[]
export const allSupportStatuses = Object.values(SupportTicketStatus) as SupportTicketStatus[]
export const allSupportPriorities = Object.values(SupportTicketPriority) as SupportTicketPriority[]

export function isSupportTicketOpen(status: SupportTicketStatus) {
  return status === SupportTicketStatus.open || status === SupportTicketStatus.inProgress
}

const areaByPathPrefix: [string, SupportArea][] = [
  ['/feed', SupportArea.feed],
  ['/eventos', SupportArea.events],
  ['/mural', SupportArea.archive],
  ['/perfil', SupportArea.profile],
  ['/bandas', SupportArea.bands],
  ['/notificacoes', SupportArea.notifications],
  ['/minha-conta', SupportArea.account],
]

export function areaFromPath(path: string | null): SupportArea | null {
  if (!path) return null
  return areaByPathPrefix.find(([prefix]) => path === prefix || path.startsWith(`${prefix}/`))?.[1] ?? null
}

export function useSupportLabels() {
  const { t } = useTranslation('suporte')
  return {
    tag: (tag: SupportTag) => t(`tags.${tagKey[tag]}`),
    area: (area: SupportArea) => t(`areas.${areaKey[area]}`),
    status: (status: SupportTicketStatus) => t(`status.${statusKey[status]}`),
    priority: (priority: SupportTicketPriority) => t(`priority.${priorityKey[priority]}`),
  }
}
