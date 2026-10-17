import { useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import { facebookLogin, loadFacebook } from '@/auth/external/facebook'
import { googleLogin, loadGoogle } from '@/auth/external/google'
import { AppButton } from '@/components/app-button'
import { cn } from '@/lib/utils'

type ProviderButtonProps = {
  onToken: (token: string) => void
  label: string
  loading?: boolean
  disabled?: boolean
  className?: string
}

function useSdk<T>(load: () => Promise<T>) {
  const [sdk, setSdk] = useState<T | null>(null)
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    let cancelled = false

    load()
      .then((loaded) => {
        if (!cancelled) setSdk(loaded)
      })
      .catch(() => {
        if (!cancelled) setFailed(true)
      })

    return () => {
      cancelled = true
    }
  }, [load])

  return { sdk, failed }
}

type ProviderButtonFrameProps = {
  label: string
  icon: ReactNode
  ready: boolean
  failedMessage: string | null
  loading: boolean
  disabled: boolean
  onClick: () => void
  className?: string
}

function ProviderButtonFrame({ label, icon, ready, failedMessage, loading, disabled, onClick, className }: ProviderButtonFrameProps) {
  return (
    <div className="flex w-full flex-col gap-1.5">
      <AppButton type="button" size="lg" block loading={loading} disabled={disabled || !ready} onClick={onClick} className={className}>
        {icon}
        {label}
      </AppButton>
      {failedMessage ? <p className="app-meta text-center">{failedMessage}</p> : null}
    </div>
  )
}

export function AppGoogleButton({ onToken, label, loading = false, disabled = false, className }: ProviderButtonProps) {
  const { t } = useTranslation('auth')
  const { sdk, failed } = useSdk(loadGoogle)

  const signIn = () => {
    if (!sdk) return
    void googleLogin(sdk).then((token) => {
      if (token) onToken(token)
    })
  }

  return (
    <ProviderButtonFrame
      label={label}
      icon={<GoogleGlyph />}
      ready={sdk !== null}
      failedMessage={failed ? t('social.googleUnavailable') : null}
      loading={loading}
      disabled={disabled}
      onClick={signIn}
      className={cn('border bg-background font-semibold text-foreground hover:bg-accent', className)}
    />
  )
}

export function AppFacebookButton({ onToken, label, loading = false, disabled = false, className }: ProviderButtonProps) {
  const { t } = useTranslation('auth')
  const { sdk, failed } = useSdk(loadFacebook)

  const signIn = () => {
    if (!sdk) return
    void facebookLogin(sdk).then((token) => {
      if (token) onToken(token)
    })
  }

  return (
    <ProviderButtonFrame
      label={label}
      icon={<FacebookGlyph />}
      ready={sdk !== null}
      failedMessage={failed ? t('social.facebookUnavailable') : null}
      loading={loading}
      disabled={disabled}
      onClick={signIn}
      className={cn('bg-facebook text-facebook-foreground hover:bg-facebook/90', className)}
    />
  )
}

function GoogleGlyph() {
  return (
    <svg viewBox="0 0 48 48" aria-hidden="true" className="size-5">
      <path fill="var(--google-red)" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z" />
      <path fill="var(--google-blue)" d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z" />
      <path fill="var(--google-yellow)" d="M10.53 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.97-6.19z" />
      <path fill="var(--google-green)" d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.15 1.45-4.92 2.3-8.16 2.3-6.26 0-11.57-4.22-13.47-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z" />
    </svg>
  )
}

function FacebookGlyph() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" className="size-5" fill="currentColor">
      <path d="M24 12.073C24 5.405 18.627 0 12 0S0 5.405 0 12.073C0 18.1 4.388 23.094 10.125 24v-8.437H7.078v-3.49h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.49h-2.796V24C19.612 23.094 24 18.1 24 12.073z" />
    </svg>
  )
}
