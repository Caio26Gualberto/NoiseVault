import { delay, mockPage } from '@/api/mock-delay'
import type { PagedResult } from '@/api/types'
import { people } from '@/api/mock-fixtures'

import { ModerationStatus } from '../moderation-status'
import type { VideoDto, VideoListQuery } from './videos-service'

function ago(days: number): string {
  return new Date(Date.now() - days * 24 * 60 * 60 * 1000).toISOString()
}

export const videos: VideoDto[] = [
  { id: 1, title: 'Fúria de Aço - Ao vivo no Porão (set completo)', description: 'Show de lançamento do EP, gravado em três câmeras.', youtubeId: 'dQw4w9WgXcQ', author: people[0], status: ModerationStatus.approved, createdAt: ago(3) },
  { id: 2, title: 'Serra Negra - Sessão de estúdio', description: 'Gravação do riff principal da faixa 2.', youtubeId: 'dQw4w9WgXcQ', author: people[1], status: ModerationStatus.approved, createdAt: ago(9) },
  { id: 3, title: 'Tamanduateí Blast - Clipe oficial', description: 'Gravado no Tamanduateí com uma câmera emprestada.', youtubeId: 'dQw4w9WgXcQ', author: people[4], status: ModerationStatus.approved, createdAt: ago(14) },
  { id: 4, title: 'Tutorial: palhetada alternada para thrash', description: 'Vinte minutos de exercício com metrônomo.', youtubeId: 'dQw4w9WgXcQ', author: people[0], status: ModerationStatus.approved, createdAt: ago(21) },
  { id: 5, title: 'Via Anchieta - Ensaio aberto (cover)', description: 'Hard rock dos anos 80 no estúdio Garagem 3.', youtubeId: 'dQw4w9WgXcQ', author: people[3], status: ModerationStatus.approved, createdAt: ago(30) },
  { id: 6, title: 'Bastidores do Festival Serra do Mar 2025', description: 'Montagem do palco e passagem de som.', youtubeId: 'dQw4w9WgXcQ', author: people[2], status: ModerationStatus.approved, createdAt: ago(45) },
  { id: 7, title: 'Paranapiacaba - Acústico na vila', description: 'Versão acústica gravada na estação.', youtubeId: 'dQw4w9WgXcQ', author: people[2], status: ModerationStatus.pending, createdAt: ago(1) },
  { id: 8, title: 'Solo de bateria - Rafa', description: 'Aquecimento antes do show.', youtubeId: 'dQw4w9WgXcQ', author: people[2], status: ModerationStatus.pending, createdAt: ago(0.5) },
]

export async function getVideos(query: VideoListQuery): Promise<PagedResult<VideoDto>> {
  await delay()
  const search = query.search.trim().toLowerCase()
  const filtered = videos
    .filter((video) => video.status === ModerationStatus.approved)
    .filter((video) => (search ? `${video.title} ${video.description}`.toLowerCase().includes(search) : true))
  return mockPage(filtered, query)
}

export async function submitVideo(url: string, title: string, description: string): Promise<VideoDto> {
  await delay()
  const match = url.match(/(?:v=|youtu\.be\/)([A-Za-z0-9_-]{6,})/)
  const video: VideoDto = {
    id: Date.now(),
    title,
    description,
    youtubeId: match?.[1] ?? 'dQw4w9WgXcQ',
    author: people[0],
    status: ModerationStatus.pending,
    createdAt: new Date().toISOString(),
  }
  videos.push(video)
  return video
}
