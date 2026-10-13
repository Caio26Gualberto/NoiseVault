import { delay, mockPage } from '@/api/mock-delay'
import type { PagedRequest, PagedResult } from '@/api/types'
import { people, picture } from '@/api/mock-fixtures'
import { ModerationStatus } from '@/pages/videos/moderation-status'

import type { PhotoDto } from './fotos-service'

function ago(days: number): string {
  return new Date(Date.now() - days * 24 * 60 * 60 * 1000).toISOString()
}

const titles = [
  ['Pogo no Porão', 'Noite do Thrash', 1],
  ['Passagem de som', 'Casa Amarela', 2],
  ['Grind no Galpão', 'Tamanduateí Blast abrindo', 3],
  ['Vista da Serra', 'Festival Serra do Mar 2025', null],
  ['Backstage', 'Fúria de Aço antes do set', 1],
  ['Vinil na feira', 'Serra do Mar 2025', null],
  ['Último acorde', 'Via Anchieta no ensaio aberto', 5],
  ['Plateia', 'Noite do Thrash', 1],
  ['Bateria do Rafa', 'Grind no Galpão', 3],
  ['Luz vermelha', 'Doom Sunday', 2],
  ['Fila da porta', 'Porão do Rock', null],
  ['Setlist', 'Serra Negra', 2],
] as const

export const photos: PhotoDto[] = titles.map(([title, subtitle, eventId], index) => ({
  id: index + 1,
  title,
  subtitle,
  description: null,
  imageUrl: picture(`photo-${index + 1}`, 900, 900),
  captureDate: ago(index * 3 + 1),
  author: people[index % people.length],
  status: index === 11 ? ModerationStatus.pending : ModerationStatus.approved,
  eventId,
}))

export async function getPhotos(query: PagedRequest): Promise<PagedResult<PhotoDto>> {
  await delay()
  return mockPage(photos.filter((photo) => photo.status === ModerationStatus.approved), query)
}
