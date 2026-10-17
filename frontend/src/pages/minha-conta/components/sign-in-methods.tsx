import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import type { SignInMethodsDto } from '@/api/types'
import { ExternalProvider, isInAppBrowser, isProviderEnabled, providerNames } from '@/auth/external/providers'
import { AppBadge } from '@/components/app-badge'
import { AppButton } from '@/components/app-button'
import { useAppConfirm } from '@/components/app-confirm'
import { AppFormPanel } from '@/components/app-form-layout'
import { AppSkeleton } from '@/components/app-skeleton'
import { AppFacebookButton, AppGoogleButton } from '@/components/app-social-sign-in'

import { linkSignInMethod, unlinkSignInMethod } from '../services/conta-service'

const providerOrder: ExternalProvider[] = [ExternalProvider.facebook, ExternalProvider.google]

type SignInMethodsPanelProps = {
  methods: SignInMethodsDto | null
  onChange: (methods: SignInMethodsDto) => void
}

export function SignInMethodsPanel({ methods, onChange }: SignInMethodsPanelProps) {
  const { t } = useTranslation('conta')
  const { t: tAuth } = useTranslation('auth')
  const confirm = useAppConfirm()
  const [busy, setBusy] = useState<ExternalProvider | null>(null)

  const providers = providerOrder.filter((provider) => isProviderEnabled(provider) || methods?.providers.includes(provider))

  if (providers.length === 0) return null

  const connect = async (provider: ExternalProvider, token: string) => {
    setBusy(provider)
    try {
      onChange(await linkSignInMethod(provider, token))
    } catch {
      return
    } finally {
      setBusy(null)
    }
  }

  const disconnect = (provider: ExternalProvider) => {
    const name = providerNames[provider]
    confirm({
      title: t('methods.disconnectTitle', { provider: name }),
      message: t('methods.disconnectMessage', { provider: name }),
      confirmLabel: t('methods.disconnect'),
      danger: true,
      onConfirm: async () => {
        try {
          onChange(await unlinkSignInMethod(provider))
        } catch {
          return
        }
      },
    })
  }

  return (
    <AppFormPanel title={t('methods.title')} description={t('methods.description')}>
      {methods ? (
        <ul className="flex flex-col divide-y rounded-lg border">
          {providers.map((provider) => {
            const connected = methods.providers.includes(provider)
            const name = providerNames[provider]

            return (
              <li key={provider} className="flex flex-wrap items-center justify-between gap-3 p-3">
                <div className="flex min-w-0 items-center gap-2">
                  <span className="text-sm font-medium">{name}</span>
                  {connected ? <AppBadge tone="success">{t('methods.connected')}</AppBadge> : null}
                </div>
                {connected ? (
                  <AppButton type="button" intent="neutral" size="sm" onClick={() => disconnect(provider)}>
                    {t('methods.disconnect')}
                  </AppButton>
                ) : provider === ExternalProvider.facebook ? (
                  <div className="w-full sm:w-60">
                    <AppFacebookButton
                      label={t('methods.connectFacebook')}
                      loading={busy === provider}
                      disabled={busy !== null}
                      onToken={(token) => void connect(provider, token)}
                    />
                  </div>
                ) : isInAppBrowser() ? (
                  <span className="app-meta">{tAuth('social.googleInApp')}</span>
                ) : (
                  <div className="w-full sm:w-60">
                    <AppGoogleButton
                      label={t('methods.connectGoogle')}
                      loading={busy === provider}
                      disabled={busy !== null}
                      onToken={(token) => void connect(provider, token)}
                    />
                  </div>
                )}
              </li>
            )
          })}
        </ul>
      ) : (
        <AppSkeleton preset="list" count={2} />
      )}
    </AppFormPanel>
  )
}
