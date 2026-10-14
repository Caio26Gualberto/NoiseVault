import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'

import { cn } from '@/lib/utils'

export type AppRichMention = {
  handle: string
  name: string
  href: string
}

type AppRichTextProps = {
  text: string
  mentions?: AppRichMention[]
  className?: string
}

const handlePattern = /(?<![a-z0-9._@-])@([a-z0-9][a-z0-9._-]{1,58}[a-z0-9])/gi

export function AppRichText({ text, mentions = [], className }: AppRichTextProps) {
  const byHandle = new Map(mentions.map((mention) => [mention.handle.toLowerCase(), mention]))
  const parts: ReactNode[] = []
  let last = 0

  if (byHandle.size > 0) {
    for (const match of text.matchAll(handlePattern)) {
      const mention = byHandle.get(match[1].toLowerCase())
      if (!mention || match.index === undefined) continue
      if (match.index > last) parts.push(text.slice(last, match.index))
      parts.push(
        <Link key={`${match.index}-${mention.handle}`} to={mention.href} className="font-medium text-primary hover:underline">
          {mention.name}
        </Link>,
      )
      last = match.index + match[0].length
    }
  }

  if (last < text.length) parts.push(text.slice(last))

  return <p className={cn('whitespace-pre-line break-words', className)}>{parts}</p>
}
