import type { AppRichMention } from '@/components/app-rich-text'
import { bandPagePath } from '@/pages/bandas/band-kind'
import type { BandKind } from '@/pages/bandas/band-kind'

export const MentionKind = { person: 0, band: 1 } as const
export type MentionKind = (typeof MentionKind)[keyof typeof MentionKind]

export type MentionDto = {
  handle: string
  kind: MentionKind
  bandKind?: BandKind | null
  id: number
  name: string
  avatarUrl: string | null
}

const mentionPath: Record<MentionKind, (mention: MentionDto) => string> = {
  [MentionKind.person]: (mention) => `/perfil/${mention.handle}`,
  [MentionKind.band]: (mention) => bandPagePath({ slug: mention.handle, kind: mention.bandKind ?? undefined }),
}

export function toRichMentions(mentions: MentionDto[] | undefined): AppRichMention[] {
  return (mentions ?? []).map((mention) => ({ handle: mention.handle, name: mention.name, href: mentionPath[mention.kind](mention) }))
}
