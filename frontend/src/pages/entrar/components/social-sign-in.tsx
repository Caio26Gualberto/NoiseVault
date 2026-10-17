import { useTranslation } from 'react-i18next'

import {
  ExternalProvider,
  isAnyProviderEnabled,
  isFacebookEnabled,
  isGoogleEnabled,
  isInAppBrowser,
} from '@/auth/external/providers'
import { AppFacebookButton, AppGoogleButton } from '@/components/app-social-sign-in'

import { useExternalSignIn } from '../hooks/use-external-sign-in'

export function SocialSignIn() {
  const { t } = useTranslation('auth')
  const { signIn, busy } = useExternalSignIn()

  if (!isAnyProviderEnabled) return null

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-col gap-3">
        {isFacebookEnabled ? (
          <AppFacebookButton
            label={t('social.facebook')}
            loading={busy === ExternalProvider.facebook}
            disabled={busy !== null}
            onToken={(token) => void signIn(ExternalProvider.facebook, token)}
          />
        ) : null}
        {isGoogleEnabled ? (
          <GoogleOption
            loading={busy === ExternalProvider.google}
            disabled={busy !== null}
            onToken={(token) => void signIn(ExternalProvider.google, token)}
          />
        ) : null}
      </div>
      <div className="flex items-center gap-3 text-[13px] text-muted-foreground">
        <span className="h-px flex-1 bg-border" aria-hidden="true" />
        {t('social.divider')}
        <span className="h-px flex-1 bg-border" aria-hidden="true" />
      </div>
    </div>
  )
}

type GoogleOptionProps = {
  onToken: (token: string) => void
  loading?: boolean
  disabled?: boolean
}

export function GoogleOption({ onToken, loading = false, disabled = false }: GoogleOptionProps) {
  const { t } = useTranslation('auth')

  if (isInAppBrowser()) {
    return <p className="app-meta text-center">{t('social.googleInApp')}</p>
  }

  return <AppGoogleButton label={t('social.google')} loading={loading} disabled={disabled} onToken={onToken} />
}
