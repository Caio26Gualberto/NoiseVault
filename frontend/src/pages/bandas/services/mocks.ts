import { delay } from '@/api/mock-delay'
import { AbcCity } from '@/pages/perfil/cities'
import { MusicGenre } from '@/pages/perfil/genres'
import type { BandSummaryDto } from '@/pages/perfil/services/perfil-service'
import { bandSummaries, people, picture } from '@/api/mock-fixtures'

import { BandMemberRole } from '../band-member-role'
import type { BandProfileDto } from './bandas-service'

const bands: Record<string, BandProfileDto> = {
  'furia-de-aco': {
    ...bandSummaries[0],
    description:
      'Thrash metal de Santo André desde 2019. Dois EPs, um disco em gravação e a missão de fazer a Vila Pires tremer.',
    city: AbcCity.santoAndre,
    genres: [MusicGenre.thrash, MusicGenre.heavy, MusicGenre.speed],
    foundedAt: '2019-05-01T00:00:00Z',
    bannerUrl: picture('banner-furia'),
    bannerPosition: 45,
    socialLinks: { instagram: 'https://instagram.com', youtube: 'https://youtube.com', spotify: 'https://spotify.com' },
    members: [
      { user: people[0], instrument: 'Guitarra', role: BandMemberRole.owner, joinedAt: '2019-05-01T00:00:00Z' },
      { user: people[4], instrument: 'Vocal', role: BandMemberRole.member, joinedAt: '2019-08-10T00:00:00Z' },
      { user: people[3], instrument: 'Baixo', role: BandMemberRole.member, joinedAt: '2021-02-15T00:00:00Z' },
      { user: people[2], instrument: 'Bateria', role: BandMemberRole.member, joinedAt: '2022-11-01T00:00:00Z' },
    ],
    stats: { posts: 36, events: 14, followers: 412 },
  },
  'serra-negra': {
    ...bandSummaries[1],
    description: 'Doom e stoner de São Bernardo. Riffs lentos, amplificadores grandes.',
    city: AbcCity.saoBernardo,
    genres: [MusicGenre.doom, MusicGenre.stoner, MusicGenre.sludge],
    foundedAt: '2021-09-01T00:00:00Z',
    bannerUrl: picture('banner-serra'),
    bannerPosition: 60,
    socialLinks: { instagram: 'https://instagram.com', youtube: null, spotify: 'https://spotify.com' },
    members: [
      { user: people[1], instrument: 'Vocal e guitarra', role: BandMemberRole.owner, joinedAt: '2021-09-01T00:00:00Z' },
      { user: people[5], instrument: 'Baixo', role: BandMemberRole.member, joinedAt: '2021-09-01T00:00:00Z' },
    ],
    stats: { posts: 12, events: 6, followers: 158 },
  },
  'tamanduatei-blast': {
    ...bandSummaries[2],
    description: 'Death e grind do Tamanduateí. Músicas curtas, shows mais curtos ainda.',
    city: AbcCity.maua,
    genres: [MusicGenre.death, MusicGenre.grindcore, MusicGenre.technicalDeath],
    foundedAt: '2017-03-01T00:00:00Z',
    bannerUrl: picture('banner-blast'),
    bannerPosition: 50,
    socialLinks: { instagram: 'https://instagram.com', youtube: 'https://youtube.com', spotify: null },
    members: [
      { user: people[2], instrument: 'Bateria', role: BandMemberRole.owner, joinedAt: '2017-03-01T00:00:00Z' },
      { user: people[4], instrument: 'Guitarra', role: BandMemberRole.member, joinedAt: '2018-01-01T00:00:00Z' },
    ],
    stats: { posts: 51, events: 22, followers: 690 },
  },
}

function fallbackBand(slug: string): BandProfileDto {
  const summary = bandSummaries.find((b) => b.slug === slug) ?? {
    id: 900,
    name: slug,
    slug,
    avatarUrl: null,
    isVerified: false,
    genres: [],
  }
  return {
    ...summary,
    description: '',
    city: AbcCity.other,
    foundedAt: null,
    bannerUrl: null,
    bannerPosition: 50,
    socialLinks: { instagram: null, youtube: null, spotify: null },
    members: [],
    stats: { posts: 0, events: 0, followers: 0 },
  }
}

export async function getBandProfile(slug: string): Promise<BandProfileDto> {
  await delay()
  return bands[slug] ?? fallbackBand(slug)
}

export async function getSuggestedBands(): Promise<BandSummaryDto[]> {
  await delay(200)
  return bandSummaries.slice(0, 4)
}
