import { ImagePlusIcon } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { useTranslation } from 'react-i18next'

import { useAuth } from '@/auth/use-auth'
import { AppAvatar } from '@/components/app-avatar'
import { AppButton, AppIconButton } from '@/components/app-button'

type PostComposerProps = {
  onPublish: (content: string) => Promise<void>
}

export function PostComposer({ onPublish }: PostComposerProps) {
  const { t } = useTranslation('feed')
  const { user } = useAuth()
  const [content, setContent] = useState('')
  const [publishing, setPublishing] = useState(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    const value = content.trim()
    if (!value) return
    setPublishing(true)
    try {
      await onPublish(value)
      setContent('')
    } finally {
      setPublishing(false)
    }
  }

  if (!user) return null

  return (
    <form onSubmit={(event) => void submit(event)} className="flex gap-3 rounded-xl border bg-card p-4">
      <AppAvatar name={user.name} size="lg" />
      <div className="flex flex-1 flex-col gap-3">
        <textarea
          value={content}
          onChange={(event) => setContent(event.target.value)}
          rows={2}
          placeholder={t('composer.placeholder', { name: user.name.split(' ')[0] })}
          aria-label={t('composer.placeholder', { name: user.name.split(' ')[0] })}
          className="min-h-14 w-full resize-none bg-transparent text-[15px] outline-none placeholder:text-muted-foreground"
        />
        <div className="flex items-center justify-between">
          <AppIconButton label={t('composer.media')} disabled>
            <ImagePlusIcon />
          </AppIconButton>
          <AppButton type="submit" size="sm" loading={publishing} disabled={!content.trim()}>
            {t('composer.publish')}
          </AppButton>
        </div>
      </div>
    </form>
  )
}
