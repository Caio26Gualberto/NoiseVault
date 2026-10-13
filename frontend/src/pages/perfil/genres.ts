import { useTranslation } from 'react-i18next'

export const MusicGenre = {
  heavy: 0,
  thrash: 1,
  death: 2,
  black: 3,
  doom: 4,
  power: 5,
  progressive: 6,
  speed: 7,
  groove: 8,
  nuMetal: 9,
  metalcore: 10,
  deathcore: 11,
  grindcore: 12,
  sludge: 13,
  stoner: 14,
  folk: 15,
  symphonic: 16,
  gothic: 17,
  industrial: 18,
  melodicDeath: 19,
  technicalDeath: 20,
  blackened: 21,
  crossover: 22,
  hardcore: 23,
  punk: 24,
  hardRock: 25,
  classicRock: 26,
  alternative: 27,
  grunge: 28,
  postMetal: 29,
  other: 30,
} as const

export type MusicGenre = (typeof MusicGenre)[keyof typeof MusicGenre]

export const musicGenreKey: Record<MusicGenre, string> = {
  [MusicGenre.heavy]: 'heavy',
  [MusicGenre.thrash]: 'thrash',
  [MusicGenre.death]: 'death',
  [MusicGenre.black]: 'black',
  [MusicGenre.doom]: 'doom',
  [MusicGenre.power]: 'power',
  [MusicGenre.progressive]: 'progressive',
  [MusicGenre.speed]: 'speed',
  [MusicGenre.groove]: 'groove',
  [MusicGenre.nuMetal]: 'nuMetal',
  [MusicGenre.metalcore]: 'metalcore',
  [MusicGenre.deathcore]: 'deathcore',
  [MusicGenre.grindcore]: 'grindcore',
  [MusicGenre.sludge]: 'sludge',
  [MusicGenre.stoner]: 'stoner',
  [MusicGenre.folk]: 'folk',
  [MusicGenre.symphonic]: 'symphonic',
  [MusicGenre.gothic]: 'gothic',
  [MusicGenre.industrial]: 'industrial',
  [MusicGenre.melodicDeath]: 'melodicDeath',
  [MusicGenre.technicalDeath]: 'technicalDeath',
  [MusicGenre.blackened]: 'blackened',
  [MusicGenre.crossover]: 'crossover',
  [MusicGenre.hardcore]: 'hardcore',
  [MusicGenre.punk]: 'punk',
  [MusicGenre.hardRock]: 'hardRock',
  [MusicGenre.classicRock]: 'classicRock',
  [MusicGenre.alternative]: 'alternative',
  [MusicGenre.grunge]: 'grunge',
  [MusicGenre.postMetal]: 'postMetal',
  [MusicGenre.other]: 'other',
}

export const allMusicGenres = Object.values(MusicGenre) as MusicGenre[]

export function useMusicGenreLabel() {
  const { t } = useTranslation('perfil')
  return (genre: MusicGenre) => t(`genres.${musicGenreKey[genre]}`)
}
