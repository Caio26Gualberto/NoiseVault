import { SendIcon } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { useTranslation } from 'react-i18next'

import { useAuth } from '@/auth/use-auth'
import { AppAvatar } from '@/components/app-avatar'
import { AppIconButton } from '@/components/app-button'

type CommentComposerProps = {
  onSubmit: (text: string) => Promise<void>
}

export function CommentComposer({ onSubmit }: CommentComposerProps) {
  const { t } = useTranslation()
  const { user } = useAuth()
  const [text, setText] = useState('')
  const [sending, setSending] = useState(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    const value = text.trim()
    if (!value) return
    setSending(true)
    try {
      await onSubmit(value)
      setText('')
    } finally {
      setSending(false)
    }
  }

  return (
    <form onSubmit={(event) => void submit(event)} className="flex items-start gap-3">
      {user ? <AppAvatar name={user.name} size="md" /> : null}
      <div className="flex flex-1 items-end gap-2 rounded-lg border bg-background px-3 py-2 focus-within:ring-2 focus-within:ring-ring/50">
        <textarea
          value={text}
          onChange={(event) => setText(event.target.value)}
          rows={1}
          placeholder={t('comments.placeholder')}
          aria-label={t('comments.placeholder')}
          className="max-h-40 min-h-6 flex-1 resize-none bg-transparent text-sm outline-none placeholder:text-muted-foreground"
        />
        <AppIconButton type="submit" label={t('comments.send')} intent="primary" size="sm" disabled={sending || !text.trim()}>
          <SendIcon />
        </AppIconButton>
      </div>
    </form>
  )
}
