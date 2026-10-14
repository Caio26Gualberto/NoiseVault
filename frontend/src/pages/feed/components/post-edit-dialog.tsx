import { useState } from 'react'
import type { FormEvent } from 'react'
import { useTranslation } from 'react-i18next'

import { AppButton } from '@/components/app-button'
import { AppDialog } from '@/components/app-dialog'

import { updatePost } from '../services/feed-service'
import type { PostDto } from '../services/feed-service'

export const postContentMaxLength = 5000

type PostEditDialogProps = {
  post: PostDto
  open: boolean
  onOpenChange: (open: boolean) => void
  onSaved: (post: PostDto) => void
}

export function PostEditDialog({ post, open, onOpenChange, onSaved }: PostEditDialogProps) {
  const { t } = useTranslation('feed')
  const [content, setContent] = useState(post.content)
  const [saving, setSaving] = useState(false)

  const value = content.trim()
  const empty = value.length === 0 && post.media.length === 0 && !post.event && !post.archive

  const change = (next: boolean) => {
    if (next) setContent(post.content)
    onOpenChange(next)
  }

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (empty) return
    setSaving(true)
    try {
      onSaved(await updatePost(post.id, value))
      onOpenChange(false)
    } catch {
      return
    } finally {
      setSaving(false)
    }
  }

  return (
    <AppDialog open={open} onOpenChange={change} title={t('post.editTitle')} description={t('post.editHint')}>
      <form onSubmit={(event) => void submit(event)} className="flex flex-col gap-3">
        <textarea
          value={content}
          onChange={(event) => setContent(event.target.value)}
          rows={6}
          maxLength={postContentMaxLength}
          aria-label={t('post.editTitle')}
          className="w-full resize-y rounded-lg border bg-background px-3 py-2 text-[15px] outline-none focus-visible:ring-2 focus-visible:ring-ring/50"
        />
        <div className="flex justify-end gap-2">
          <AppButton type="button" intent="ghost" onClick={() => change(false)}>
            {t('post.cancel')}
          </AppButton>
          <AppButton type="submit" loading={saving} disabled={empty || value === post.content}>
            {t('post.save')}
          </AppButton>
        </div>
      </form>
    </AppDialog>
  )
}
