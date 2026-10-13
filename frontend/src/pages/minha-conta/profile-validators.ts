import { z } from 'zod'

import { nameSchema } from '@/auth/validators'
import { allAbcCities } from '@/pages/perfil/cities'
import { allMusicGenres } from '@/pages/perfil/genres'

export const bioMaxLength = 500
export const instrumentMaxLength = 60
export const socialLinkMaxLength = 300
export const maxGenres = 10

type SocialNetwork = 'instagram' | 'youtube' | 'spotify'

const allowedHosts: Record<SocialNetwork, string[]> = {
  instagram: ['instagram.com'],
  youtube: ['youtube.com', 'youtu.be'],
  spotify: ['open.spotify.com', 'spotify.com'],
}

export function isValidSocialLink(network: SocialNetwork, value: string): boolean {
  if (value.length > socialLinkMaxLength) return false
  let url: URL
  try {
    url = new URL(value)
  } catch {
    return false
  }
  if (url.protocol !== 'https:' && url.protocol !== 'http:') return false
  const host = url.hostname.toLowerCase().replace(/^(www|m)\./, '')
  return allowedHosts[network].includes(host)
}

const socialLinkSchema = (network: SocialNetwork) =>
  z
    .string()
    .trim()
    .refine((value) => value === '' || isValidSocialLink(network, value), 'validation.socialLink')

export const profileFormSchema = z.object({
  name: nameSchema,
  bio: z.string().trim().max(bioMaxLength, 'validation.bioLength'),
  city: z.string().refine((value) => value === '' || allAbcCities.includes(Number(value) as never), 'validation.required'),
  mainInstrument: z.string().trim().max(instrumentMaxLength, 'validation.instrumentLength'),
  genres: z
    .array(z.number().refine((value) => allMusicGenres.includes(value as never)))
    .max(maxGenres, 'validation.genresMax'),
  instagram: socialLinkSchema('instagram'),
  youtube: socialLinkSchema('youtube'),
  spotify: socialLinkSchema('spotify'),
})

export type ProfileFormValues = z.infer<typeof profileFormSchema>
