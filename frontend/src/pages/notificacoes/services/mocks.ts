import { delay, mockPage } from '@/api/mock-delay'
import type { PagedRequest, PagedResult } from '@/api/types'
import { picture } from '@/pages/perfil/services/mocks'

import { NotificationType } from '../notification-type'
import type { NotificationDto } from './notificacoes-service'

function ago(hours: number): string {
  return new Date(Date.now() - hours * 60 * 60 * 1000).toISOString()
}

const notifications: NotificationDto[] = [
  {
    id: 1,
    type: NotificationType.bandInvite,
    title: 'Convite para a Serra Negra',
    message: 'Marina Vitorino convidou você para tocar guitarra na Serra Negra.',
    link: '/bandas/serra-negra',
    imageUrl: null,
    readAt: null,
    createdAt: ago(1),
    invitation: { id: 10, bandId: 2, bandName: 'Serra Negra', bandSlug: 'serra-negra', instrument: 'Guitarra', status: 'pending' },
  },
  {
    id: 2,
    type: NotificationType.simple,
    title: 'Rafael Tenório comentou no seu post',
    message: '"Possuído e com dor no braço, mas feliz."',
    link: '/feed/1',
    imageUrl: null,
    readAt: null,
    createdAt: ago(2),
    invitation: null,
  },
  {
    id: 3,
    type: NotificationType.banner,
    title: 'Festival Serra do Mar: ingressos abertos',
    message: 'Lote promocional até sexta. A Fúria de Aço está no line-up.',
    link: '/eventos/4',
    imageUrl: picture('notif-festival', 1200, 400),
    readAt: null,
    createdAt: ago(6),
    invitation: null,
  },
  {
    id: 4,
    type: NotificationType.simple,
    title: 'Seu vídeo foi aprovado',
    message: '"Tutorial: palhetada alternada para thrash" já está na galeria.',
    link: '/videos',
    imageUrl: null,
    readAt: ago(20),
    createdAt: ago(24),
    invitation: null,
  },
  {
    id: 5,
    type: NotificationType.popup,
    title: 'Bem-vindo ao Headbanger',
    message: 'Complete seu perfil com cidade, instrumento e gêneros para aparecer nas buscas.',
    link: '/minha-conta',
    imageUrl: null,
    readAt: ago(70),
    createdAt: ago(72),
    invitation: null,
  },
]

export async function getNotifications(query: PagedRequest): Promise<PagedResult<NotificationDto>> {
  await delay()
  return mockPage([...notifications].sort((a, b) => b.createdAt.localeCompare(a.createdAt)), query)
}

export async function getUnreadCount(): Promise<number> {
  await delay(100)
  return notifications.filter((n) => n.readAt === null).length
}

export async function markRead(id: number): Promise<void> {
  await delay(100)
  const item = notifications.find((n) => n.id === id)
  if (item && item.readAt === null) item.readAt = new Date().toISOString()
}

export async function markAllRead(): Promise<void> {
  await delay(150)
  const now = new Date().toISOString()
  for (const item of notifications) item.readAt ??= now
}

export async function remove(id: number): Promise<void> {
  await delay(100)
  const index = notifications.findIndex((n) => n.id === id)
  if (index >= 0) notifications.splice(index, 1)
}

export async function respondInvitation(id: number, accept: boolean): Promise<void> {
  await delay(200)
  const item = notifications.find((n) => n.id === id)
  if (!item?.invitation) return
  item.invitation.status = accept ? 'accepted' : 'declined'
  item.readAt ??= new Date().toISOString()
}
