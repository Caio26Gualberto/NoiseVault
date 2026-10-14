import { HeadsetIcon, SendIcon } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { useTranslation } from 'react-i18next'

import { AppAvatar } from '@/components/app-avatar'
import { AppBadge } from '@/components/app-badge'
import { AppIconButton } from '@/components/app-button'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { formatRelative } from '@/lib/format'
import { cn } from '@/lib/utils'

import type { useTicketMessages } from '../hooks/use-ticket-messages'

const messageMaxLength = 2000

type TicketConversationProps = {
  messages: ReturnType<typeof useTicketMessages>
  canSend: boolean
  closedNotice?: string | null
  hint?: string | null
  placeholder: string
  emptyText: string
}

export function TicketConversation({ messages, canSend, closedNotice, hint, placeholder, emptyText }: TicketConversationProps) {
  const { t } = useTranslation('suporte')
  const [text, setText] = useState('')
  const [sending, setSending] = useState(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    const value = text.trim()
    if (!value) return
    setSending(true)
    try {
      await messages.reply(value)
      setText('')
    } finally {
      setSending(false)
    }
  }

  return (
    <section className="flex flex-col gap-4">
      <h2 className="app-h3">{t('conversation.title')}</h2>

      {messages.loading && messages.items.length === 0 ? (
        <AppSkeleton preset="list" count={2} />
      ) : messages.items.length === 0 ? (
        <p className="text-sm text-muted-foreground">{emptyText}</p>
      ) : (
        <ol className="flex flex-col gap-3">
          {messages.items.map((message) => (
            <li key={message.id} className={cn('flex gap-3', message.mine && 'flex-row-reverse')}>
              <AppAvatar name={message.author.name} src={message.author.avatarUrl} size="md" />
              <div
                className={cn(
                  'flex max-w-[85%] flex-col gap-1 rounded-xl border px-3 py-2',
                  message.fromStaff ? 'border-primary/30 bg-primary/[0.06]' : 'bg-card',
                )}
              >
                <span className="flex flex-wrap items-center gap-2 text-sm font-medium">
                  {message.author.name}
                  {message.fromStaff ? (
                    <AppBadge tone="brand">
                      <HeadsetIcon />
                      {t('conversation.staff')}
                    </AppBadge>
                  ) : null}
                </span>
                <p className="text-sm whitespace-pre-line break-words">{message.text}</p>
                <span className="app-meta">{formatRelative(message.createdAt)}</span>
              </div>
            </li>
          ))}
        </ol>
      )}

      <AppPagination
        page={messages.page}
        totalPages={messages.totalPages}
        totalCount={messages.totalCount}
        onChange={messages.setPage}
        mode="loadMore"
        loading={messages.loading}
      />

      {canSend ? (
        <form onSubmit={(event) => void submit(event)} className="flex flex-col gap-2">
          <div className="flex items-end gap-2 rounded-lg border bg-background px-3 py-2 focus-within:ring-2 focus-within:ring-ring/50">
            <textarea
              value={text}
              onChange={(event) => setText(event.target.value)}
              rows={2}
              maxLength={messageMaxLength}
              placeholder={placeholder}
              aria-label={placeholder}
              className="max-h-48 min-h-10 flex-1 resize-none bg-transparent text-sm outline-none placeholder:text-muted-foreground"
            />
            <AppIconButton type="submit" label={t('conversation.send')} intent="primary" size="sm" disabled={sending || !text.trim()}>
              <SendIcon />
            </AppIconButton>
          </div>
          {hint ? <p className="app-meta">{hint}</p> : null}
        </form>
      ) : closedNotice ? (
        <p className="rounded-lg border border-dashed p-3 text-sm text-muted-foreground">{closedNotice}</p>
      ) : null}
    </section>
  )
}
