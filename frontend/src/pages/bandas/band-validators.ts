import { z } from 'zod'

import { nameSchema } from '@/auth/validators'
import { instrumentMaxLength, isValidSocialLink, maxGenres } from '@/pages/minha-conta/profile-validators'
import { cityValueSchema, emptyCityValue, toCityInput, toCityValue } from '@/pages/perfil/city'
import { allMusicGenres } from '@/pages/perfil/genres'
import type { MusicGenre } from '@/pages/perfil/genres'

import type { BandInput, BandProfileDto } from './services/bandas-service'

export const bandDescriptionMaxLength = 1000
export const minFoundedYear = 1950

const socialLink = (network: 'instagram' | 'youtube' | 'spotify') =>
  z
    .string()
    .trim()
    .refine((value) => value === '' || isValidSocialLink(network, value), 'validation.socialLink')

export const bandFormSchema = z.object({
  name: nameSchema,
  description: z.string().trim().max(bandDescriptionMaxLength, 'validation.bandDescriptionLength'),
  city: cityValueSchema,
  foundedYear: z
    .string()
    .trim()
    .refine((value) => {
      if (value === '') return true
      const year = Number(value)
      return Number.isInteger(year) && year >= minFoundedYear && year <= new Date().getFullYear()
    }, 'validation.foundedYear'),
  instrument: z.string().trim().max(instrumentMaxLength, 'validation.instrumentLength'),
  genres: z
    .array(z.number().refine((value) => allMusicGenres.includes(value as never)))
    .max(maxGenres, 'validation.genresMax'),
  instagram: socialLink('instagram'),
  youtube: socialLink('youtube'),
  spotify: socialLink('spotify'),
})

export type BandFormValues = z.infer<typeof bandFormSchema>

const orNull = (value: string) => (value === '' ? null : value)

export const emptyBandForm: BandFormValues = {
  name: '',
  description: '',
  city: emptyCityValue,
  foundedYear: '',
  instrument: '',
  genres: [],
  instagram: '',
  youtube: '',
  spotify: '',
}

export function toBandForm(band: BandProfileDto): BandFormValues {
  return {
    name: band.name,
    description: band.description ?? '',
    city: toCityValue(band),
    foundedYear: band.foundedYear === null ? '' : String(band.foundedYear),
    instrument: '',
    genres: band.genres,
    instagram: band.socialLinks.instagram ?? '',
    youtube: band.socialLinks.youtube ?? '',
    spotify: band.socialLinks.spotify ?? '',
  }
}

export function toBandInput(values: BandFormValues): BandInput {
  return {
    name: values.name,
    description: orNull(values.description),
    ...toCityInput(values.city),
    foundedYear: values.foundedYear === '' ? null : Number(values.foundedYear),
    genres: values.genres as MusicGenre[],
    socialLinks: {
      instagram: orNull(values.instagram),
      youtube: orNull(values.youtube),
      spotify: orNull(values.spotify),
    },
  }
}

export function instrumentOf(values: BandFormValues): string | null {
  return orNull(values.instrument)
}
