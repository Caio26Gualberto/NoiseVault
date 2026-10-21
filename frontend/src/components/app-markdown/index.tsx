import type { ReactNode } from 'react'
import Markdown from 'react-markdown'
import type { Components } from 'react-markdown'
import remarkBreaks from 'remark-breaks'

import { cn } from '@/lib/utils'

const allowedElements = ['p', 'br', 'strong', 'em', 'del', 'a', 'ul', 'ol', 'li', 'blockquote', 'code', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'hr']

const heading = ({ children }: { children?: ReactNode }) => <h3 className="app-h3 mt-2">{children}</h3>

const components: Components = {
  h1: heading,
  h2: heading,
  h3: heading,
  h4: heading,
  h5: heading,
  h6: heading,
  a: ({ href, children }) => (
    <a href={href} target="_blank" rel="noopener noreferrer nofollow ugc" className="text-primary underline underline-offset-2">
      {children}
    </a>
  ),
  strong: ({ children }) => <strong className="font-semibold">{children}</strong>,
  ul: ({ children }) => <ul className="list-disc space-y-1 pl-5">{children}</ul>,
  ol: ({ children }) => <ol className="list-decimal space-y-1 pl-5">{children}</ol>,
  blockquote: ({ children }) => <blockquote className="border-l-2 pl-3 text-muted-foreground">{children}</blockquote>,
  code: ({ children }) => <code className="rounded bg-muted px-1 py-0.5 text-[0.9em]">{children}</code>,
  hr: () => <hr className="border-border" />,
}

type AppMarkdownProps = {
  text: string
  className?: string
}

export function AppMarkdown({ text, className }: AppMarkdownProps) {
  return (
    <div className={cn('flex flex-col gap-3 break-words', className)}>
      <Markdown remarkPlugins={[remarkBreaks]} allowedElements={allowedElements} unwrapDisallowed components={components}>
        {text}
      </Markdown>
    </div>
  )
}
