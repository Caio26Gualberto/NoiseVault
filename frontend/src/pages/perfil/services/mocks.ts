import { delay } from '@/api/mock-delay'

import { AbcCity } from '../cities'
import { MusicGenre } from '../genres'
import type { BandSummaryDto, ProfileSummaryDto, UserProfileDto } from './perfil-service'

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
  { id: 2, name: 'Caio Gualberto', nickname: 'caio', avatarUrl: avatar('caio'), isVerified: false },
  { id: 3, name: 'Marina Vitorino', nickname: 'marina.v', avatarUrl: avatar('marina'), isVerified: false },
  { id: 4, name: 'Rafael Tenório', nickname: 'rafa_tenorio', avatarUrl: avatar('rafa'), isVerified: true },
  { id: 5, name: 'Bia Cordeiro', nickname: 'bia.cordeiro', avatarUrl: avatar('bia'), isVerified: false },
  { id: 6, name: 'Dudu Lacerda', nickname: 'dudulacerda', avatarUrl: avatar('dudu'), isVerified: false },
  { id: 7, name: 'Lívia Sampaio', nickname: 'livia', avatarUrl: avatar('livia'), isVerified: false },
]

const profiles: Record<string, UserProfileDto> = {
  caio: {
    ...people[0],
    bio: 'Guitarra e produção. Toco na Fúria de Aço e gravo bandas do ABC no estúdio em Santo André.',
    city: AbcCity.santoAndre,
    instrument: 'Guitarra',
    genres: [MusicGenre.thrash, MusicGenre.heavy, MusicGenre.progressive],
    bannerUrl: picture('banner-caio'),
    bannerPosition: 40,
    socialLinks: { instagram: 'https://instagram.com', youtube: 'https://youtube.com', spotify: null },
    bands: [bandSummaries[0]],
    stats: { posts: 24, photos: 58, videos: 7 },
    memberSince: '2024-03-12T00:00:00Z',
  },
  'marina.v': {
    ...people[1],
    bio: 'Vocal da Serra Negra. Doom lento e café forte.',
    city: AbcCity.saoBernardo,
    instrument: 'Vocal',
    genres: [MusicGenre.doom, MusicGenre.stoner, MusicGenre.sludge],
    bannerUrl: picture('banner-marina'),
    bannerPosition: 55,
    socialLinks: { instagram: 'https://instagram.com', youtube: null, spotify: 'https://spotify.com' },
    bands: [bandSummaries[1]],
    stats: { posts: 11, photos: 32, videos: 3 },
    memberSince: '2024-07-02T00:00:00Z',
  },
  rafa_tenorio: {
    ...people[2],
    bio: 'Bateria. Fotografo shows quando não estou tocando.',
    city: AbcCity.maua,
    instrument: 'Bateria',
    genres: [MusicGenre.death, MusicGenre.grindcore],
    bannerUrl: picture('banner-rafa'),
    bannerPosition: 50,
    socialLinks: { instagram: 'https://instagram.com', youtube: null, spotify: null },
    bands: [bandSummaries[2], bandSummaries[4]],
    stats: { posts: 40, photos: 210, videos: 12 },
    memberSince: '2023-11-20T00:00:00Z',
  },
}

function fallbackProfile(nickname: string): UserProfileDto {
  const summary = people.find((p) => p.nickname === nickname) ?? {
    id: 100,
    name: nickname,
    nickname,
    avatarUrl: null,
    isVerified: false,
  }
  return {
    ...summary,
    bio: null,
    city: AbcCity.other,
    instrument: null,
    genres: [],
    bannerUrl: null,
    bannerPosition: 50,
    socialLinks: { instagram: null, youtube: null, spotify: null },
    bands: [],
    stats: { posts: 0, photos: 0, videos: 0 },
    memberSince: new Date().toISOString(),
  }
}

export async function getUserProfile(nickname: string): Promise<UserProfileDto> {
  await delay()
  return profiles[nickname] ?? fallbackProfile(nickname)
}

export async function getProfileSummary(nickname: string): Promise<ProfileSummaryDto | null> {
  await delay(100)
  return people.find((p) => p.nickname === nickname) ?? null
}
