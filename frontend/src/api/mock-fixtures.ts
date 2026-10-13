import { MusicGenre } from '@/pages/perfil/genres'
import type { BandSummaryDto } from '@/pages/bandas/services/bandas-service'
import type { ProfileSummaryDto } from '@/pages/perfil/services/perfil-service'

export const avatar = (seed: string) => `https://api.dicebear.com/9.x/thumbs/svg?seed=${seed}&backgroundType=gradientLinear`
export const picture = (seed: string, w = 1200, h = 400) => `https://picsum.photos/seed/${seed}/${w}/${h}`

export const bandSummaries: BandSummaryDto[] = [
  { id: 1, name: 'Fúria de Aço', slug: 'furia-de-aco', avatarUrl: avatar('furia'), isVerified: true, genres: [MusicGenre.thrash, MusicGenre.heavy] },
  { id: 2, name: 'Serra Negra', slug: 'serra-negra', avatarUrl: avatar('serra'), isVerified: false, genres: [MusicGenre.doom, MusicGenre.stoner] },
  { id: 3, name: 'Tamanduateí Blast', slug: 'tamanduatei-blast', avatarUrl: avatar('blast'), isVerified: true, genres: [MusicGenre.death, MusicGenre.grindcore] },
  { id: 4, name: 'Via Anchieta', slug: 'via-anchieta', avatarUrl: avatar('anchieta'), isVerified: false, genres: [MusicGenre.hardRock, MusicGenre.classicRock] },
  { id: 5, name: 'Paranapiacaba', slug: 'paranapiacaba', avatarUrl: avatar('parana'), isVerified: true, genres: [MusicGenre.black, MusicGenre.folk] },
]

export const people: ProfileSummaryDto[] = [
  { id: 2, name: 'Caio Gualberto', nickname: 'caio', avatarUrl: avatar('caio') },
  { id: 3, name: 'Marina Vitorino', nickname: 'marina.v', avatarUrl: avatar('marina') },
  { id: 4, name: 'Rafael Tenório', nickname: 'rafa_tenorio', avatarUrl: avatar('rafa') },
  { id: 5, name: 'Bia Cordeiro', nickname: 'bia.cordeiro', avatarUrl: avatar('bia') },
  { id: 6, name: 'Dudu Lacerda', nickname: 'dudulacerda', avatarUrl: avatar('dudu') },
  { id: 7, name: 'Lívia Sampaio', nickname: 'livia', avatarUrl: avatar('livia') },
]
