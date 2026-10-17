import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { facebookLogin, loadFacebook } from '@/auth/external/facebook'
import type { FacebookSdk } from '@/auth/external/facebook'
import { renderGoogleButton } from '@/auth/external/google'
import { AppButton } from '@/components/app-button'
import { cn } from '@/lib/utils'
import { useColorScheme } from '@/theme/color-scheme'

type GoogleButtonText = 'continue_with' | 'signin_with'

type AppGoogleButtonProps = {
  onToken: (token: string) => void
  text?: GoogleButtonText
  disabled?: boolean
  className?: string
}

const googleMinWidth = 200
const googleMaxWidth = 400

export function AppGoogleButton({ onToken, text = 'continue_with', disabled = false, className }: AppGoogleButtonProps) {
  const { t } = useTranslation('auth')
  const { isDark } = useColorScheme()
  const container = useRef<HTMLDivElement>(null)
  const handler = useRef(onToken)
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    handler.current = onToken
  })

  useEffect(() => {
    const element = container.current
    if (!element) return

    let cancelled = false
    const width = Math.max(googleMinWidth, Math.min(googleMaxWidth, Math.floor(element.getBoundingClientRect().width)))

    renderGoogleButton(
      element,
      {
        type: 'standard',
        theme: isDark ? 'filled_black' : 'outline',
        size: 'large',
        text,
        shape: 'rectangular',
        logo_alignment: 'center',
        width,
        locale: 'pt-BR',
      },
      (token) => {
        if (!cancelled) handler.current(token)
      },
    ).catch(() => {
      if (!cancelled) setFailed(true)
    })

    return () => {
      cancelled = true
    }
  }, [isDark, text])

  if (failed) {
    return <p className="app-meta text-center">{t('social.googleUnavailable')}</p>
  }

  return (
    <div
      ref={container}
      style={{ colorScheme: 'light' }}
      aria-busy={disabled || undefined}
      className={cn('flex min-h-11 w-full items-center justify-center', disabled && 'pointer-events-none opacity-60', className)}
    />
  )
}

type AppFacebookButtonProps = {
  onToken: (token: string) => void
  label: string
  loading?: boolean
  disabled?: boolean
  className?: string
}

export function AppFacebookButton({ onToken, label, loading = false, disabled = false, className }: AppFacebookButtonProps) {
  const { t } = useTranslation('auth')
  const [sdk, setSdk] = useState<FacebookSdk | null>(null)
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    let cancelled = false

    loadFacebook()
      .then((loaded) => {
        if (!cancelled) setSdk(loaded)
      })
      .catch(() => {
        if (!cancelled) setFailed(true)
      })

    return () => {
      cancelled = true
    }
  }, [])

  const signIn = () => {
    if (!sdk) return
    void facebookLogin(sdk).then((token) => {
      if (token) onToken(token)
    })
  }

  return (
    <div className="flex w-full flex-col gap-1.5">
      <AppButton
        type="button"
        size="lg"
        block
        loading={loading}
        disabled={disabled || !sdk}
        onClick={signIn}
        className={cn('bg-facebook text-facebook-foreground hover:bg-facebook/90', className)}
      >
        <FacebookGlyph />
        {label}
      </AppButton>
      {failed ? <p className="app-meta text-center">{t('social.facebookUnavailable')}</p> : null}
    </div>
  )
}

function FacebookGlyph() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" className="size-5" fill="currentColor">
      <path d="M24 12.073C24 5.405 18.627 0 12 0S0 5.405 0 12.073C0 18.1 4.388 23.094 10.125 24v-8.437H7.078v-3.49h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.49h-2.796V24C19.612 23.094 24 18.1 24 12.073z" />
    </svg>
  )
}
