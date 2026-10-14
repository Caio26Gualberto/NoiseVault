import type { AppRichMention } from '@/components/app-rich-text'

export const MentionKind = { person: 0, band: 1 } as const
export type MentionKind = (typeof MentionKind)[keyof typeof MentionKind]

export type MentionDto = {
  handle: string
  kind: MentionKind
  id: number
  name: string
  avatarUrl: string | null
}

const mentionPath: Record<MentionKind, (handle: string) => string> = {
  [MentionKind.person]: (handle) => `/perfil/${handle}`,
  [MentionKind.band]: (handle) => `/bandas/${handle}`,
}

export function toRichMentions(mentions: MentionDto[] | undefined): AppRichMention[] {
  return (mentions ?? []).map((mention) => ({ handle: mention.handle, name: mention.name, href: mentionPath[mention.kind](mention.handle) }))
}
