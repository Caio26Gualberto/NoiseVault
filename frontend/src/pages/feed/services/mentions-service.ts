import type { AppMentionOption } from '@/components/app-mention-textarea'
import { searchBands } from '@/pages/bandas/services/bandas-service'
import { searchPeople } from '@/pages/perfil/services/perfil-service'

export async function searchMentions(term: string): Promise<AppMentionOption[]> {
  const [people, bands] = await Promise.all([searchPeople(term, 4).catch(() => null), searchBands(term, 3).catch(() => null)])

  return [
    ...(people?.items ?? []).map((person) => ({ kind: 'person' as const, handle: person.nickname, name: person.name, avatarUrl: person.avatarUrl })),
    ...(bands?.items ?? []).map((band) => ({ kind: 'band' as const, handle: band.slug, name: band.name, avatarUrl: band.avatarUrl, verified: band.isVerified })),
  ]
}

export async function searchTaggablePeople(term: string): Promise<AppMentionOption[]> {
  const people = await searchPeople(term, 6)
  return people.items.map((person) => ({ kind: 'person' as const, handle: person.nickname, name: person.name, avatarUrl: person.avatarUrl }))
}
