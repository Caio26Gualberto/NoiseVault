import { zodResolver } from '@hookform/resolvers/zod'
import { Link2Icon } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link, useLocation } from 'react-router-dom'
import { z } from 'zod'

import { ApiCodes } from '@/api/api-codes'
import { ApiError } from '@/api/types'
import { externalLink } from '@/auth/auth-service'
import { ExternalProvider, isGoogleEnabled, providerNames } from '@/auth/external/providers'
import { forgotPasswordPath } from '@/auth/routes'
import { useAuth } from '@/auth/use-auth'
import { AppButton } from '@/components/app-button'
import { AppForm, AppPasswordField } from '@/components/app-field'

import { ExternalStepExpired } from './components/external-step-expired'
import { GoogleOption } from './components/social-sign-in'
import { readExternalStep } from './hooks/external-step'
import { useExternalSignIn } from './hooks/use-external-sign-in'
import { useFinishSignIn } from './hooks/use-finish-sign-in'

const schema = z.object({
  password: z.string().min(1, 'validation.required'),
})

type VincularValues = z.infer<typeof schema>

export function VincularContaPage() {
  const { t } = useTranslation('auth')
  const location = useLocation()
  const step = readExternalStep(location.state)
  const { acceptToken } = useAuth()
  const finish = useFinishSignIn()
  const { signIn, busy } = useExternalSignIn()
  const [submitting, setSubmitting] = useState(false)
  const [expired, setExpired] = useState(false)

  const form = useForm<VincularValues>({
    resolver: zodResolver(schema),
    defaultValues: { password: '' },
  })

  if (!step || expired) return <ExternalStepExpired />

  const provider = providerNames[step.provider]

  const submit = async ({ password }: VincularValues) => {
    setSubmitting(true)
    try {
      const result = await externalLink(step.ticket, password)
      if (!result.tokens) return
      await acceptToken(result.tokens.token)
      finish()
    } catch (error) {
      if (error instanceof ApiError && error.code === ApiCodes.externalTicketInvalid) setExpired(true)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col items-center gap-3 text-center">
        <div className="flex size-14 items-center justify-center rounded-full bg-info/15 text-info">
          <Link2Icon className="size-7" />
        </div>
        <div className="flex flex-col gap-1">
          <h1 className="app-h2">{t('external.linkTitle')}</h1>
          <p className="text-sm text-muted-foreground">{t('external.linkSubtitle', { email: step.email, provider })}</p>
        </div>
      </div>

      {step.hasPassword ? (
        <AppForm form={form} onSubmit={submit}>
          <div className="flex flex-col gap-2">
            <AppPasswordField name="password" label={t('fields.password')} autoComplete="current-password" />
            <Link to={forgotPasswordPath} className="self-end text-[13px] text-primary hover:underline">
              {t('login.forgot')}
            </Link>
          </div>
          <AppButton type="submit" size="lg" block loading={submitting}>
            {t('external.linkSubmit', { provider })}
          </AppButton>
        </AppForm>
      ) : (
        <div className="flex flex-col gap-4">
          <p className="text-sm text-muted-foreground">{t('external.linkWithoutPassword', { provider })}</p>
          {isGoogleEnabled && step.provider !== ExternalProvider.google ? (
            <GoogleOption loading={busy === ExternalProvider.google} disabled={busy !== null} onToken={(token) => void signIn(ExternalProvider.google, token)} />
          ) : null}
        </div>
      )}
    </div>
  )
}
