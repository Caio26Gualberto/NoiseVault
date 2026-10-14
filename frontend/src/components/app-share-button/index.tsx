import { Share2Icon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'

import { AppButton, AppIconButton } from '@/components/app-button'

type AppShareButtonProps = {
  path: string
  title: string
  label?: boolean
  size?: 'sm' | 'md'
  className?: string
}

export function shareUrlFor(path: string) {
  return `${window.location.origin}/s${path}`
}

export function AppShareButton({ path, title, label = false, size = 'sm', className }: AppShareButtonProps) {
  const { t } = useTranslation()

  const share = async () => {
    const url = shareUrlFor(path)
    const touch = window.matchMedia('(pointer: coarse)').matches

    if (touch && typeof navigator.share === 'function') {
      try {
        await navigator.share({ title, url })
      } catch {
        return
      }
      return
    }

    try {
      await navigator.clipboard.writeText(url)
      toast.success(t('share.copied'), { description: t('share.copiedHint') })
    } catch {
      toast.error(t('share.copyFailed'), { description: url })
    }
  }

  if (label) {
    return (
      <AppButton intent="neutral" size={size} onClick={() => void share()} className={className}>
        <Share2Icon />
        {t('share.action')}
      </AppButton>
    )
  }

  return (
    <AppIconButton label={t('share.action')} size={size} onClick={() => void share()} className={className}>
      <Share2Icon />
    </AppIconButton>
  )
}
