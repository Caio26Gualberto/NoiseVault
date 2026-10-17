import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { useLocation } from 'react-router-dom'
import { z } from 'zod'

import { ApiCodes } from '@/api/api-codes'
import { ApiError } from '@/api/types'
import { externalRegister } from '@/auth/auth-service'
import { providerNames } from '@/auth/external/providers'
import { useAuth } from '@/auth/use-auth'
import { emailSchema, nameSchema, nicknameSchema } from '@/auth/validators'
import { AppButton } from '@/components/app-button'
import { AppField, AppForm } from '@/components/app-field'

import { ExternalStepExpired } from './components/external-step-expired'
import { readExternalStep } from './hooks/external-step'
import { useFinishSignIn } from './hooks/use-finish-sign-in'

const withEmail = z.object({ name: nameSchema, nickname: nicknameSchema, email: emailSchema })
const withoutEmail = z.object({ name: nameSchema, nickname: nicknameSchema, email: z.string() })

type ApelidoValues = z.infer<typeof withEmail>

export function EscolherApelidoPage() {
  const { t } = useTranslation('auth')
  const location = useLocation()
  const step = readExternalStep(location.state)
  const { acceptToken } = useAuth()
  const finish = useFinishSignIn()
  const [submitting, setSubmitting] = useState(false)
  const [expired, setExpired] = useState(false)
  const needsEmail = !step?.email

  const form = useForm<ApelidoValues>({
    resolver: zodResolver(needsEmail ? withEmail : withoutEmail),
    defaultValues: { name: step?.name ?? '', nickname: step?.suggestedNickname ?? '', email: '' },
  })

  const nickname = useWatch({ control: form.control, name: 'nickname' })

  if (!step || expired) return <ExternalStepExpired />

  const provider = providerNames[step.provider]

  const submit = async (values: ApelidoValues) => {
    setSubmitting(true)
    try {
      const result = await externalRegister({
        ticket: step.ticket,
        name: values.name,
        nickname: values.nickname,
        email: needsEmail ? values.email : null,
      })
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
      <div className="flex flex-col gap-1">
        <h1 className="app-h2">{t('external.signupTitle')}</h1>
        <p className="text-sm text-muted-foreground">{t('external.signupSubtitle', { provider })}</p>
      </div>

      <AppForm form={form} onSubmit={submit}>
        <AppField name="name" label={t('fields.name')} autoComplete="name" placeholder={t('fields.namePlaceholder')} />
        <AppField
          name="nickname"
          label={t('fields.nickname')}
          autoComplete="username"
          autoCapitalize="none"
          spellCheck={false}
          placeholder={t('fields.nicknamePlaceholder')}
          hint={t('fields.nicknameTooltip')}
          description={nickname ? t('fields.nicknameHint', { nickname: nickname.toLowerCase() }) : undefined}
        />
        {needsEmail ? (
          <AppField
            name="email"
            label={t('fields.email')}
            type="email"
            autoComplete="email"
            inputMode="email"
            placeholder={t('fields.emailPlaceholder')}
            description={t('external.emailMissing', { provider })}
          />
        ) : (
          <p className="text-[13px] text-muted-foreground">{t('external.emailFromProvider', { provider, email: step.email })}</p>
        )}
        <AppButton type="submit" size="lg" block loading={submitting}>
          {t('external.signupSubmit')}
        </AppButton>
      </AppForm>
    </div>
  )
}
