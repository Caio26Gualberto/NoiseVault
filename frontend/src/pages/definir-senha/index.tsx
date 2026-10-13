import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { z } from 'zod'

import { tokenStorage } from '@/api/token-storage'
import { setInitialPassword } from '@/auth/auth-service'
import { homePath } from '@/auth/routes'
import { useAuth } from '@/auth/use-auth'
import { passwordSchema, withPasswordConfirmation } from '@/auth/validators'
import { AppButton } from '@/components/app-button'
import { AppForm, AppPasswordField } from '@/components/app-field'
import { PublicFrame } from '@/layout/public-layout'

const schema = withPasswordConfirmation(
  z.object({
    password: passwordSchema,
    confirmPassword: z.string(),
  }),
)

type Values = z.infer<typeof schema>

export function DefinirSenhaPage() {
  const { t } = useTranslation('auth')
  const { refreshUser } = useAuth()
  const navigate = useNavigate()
  const [submitting, setSubmitting] = useState(false)
  const form = useForm<Values>({ resolver: zodResolver(schema), defaultValues: { password: '', confirmPassword: '' } })

  const submit = async ({ password }: Values) => {
    setSubmitting(true)
    try {
      const tokens = await setInitialPassword(password)
      tokenStorage.saveTokens(tokens.token, tokens.refreshToken)
      await refreshUser()
      navigate(homePath, { replace: true })
    } catch {
      return
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <PublicFrame>
      <div className="flex flex-col gap-6">
        <div className="flex flex-col gap-1">
          <h1 className="app-h2">{t('initialPassword.title')}</h1>
          <p className="text-sm text-muted-foreground">{t('initialPassword.subtitle')}</p>
        </div>
        <AppForm form={form} onSubmit={submit}>
          <AppPasswordField name="password" label={t('fields.newPassword')} autoComplete="new-password" />
          <AppPasswordField name="confirmPassword" label={t('fields.confirmPassword')} autoComplete="new-password" />
          <AppButton type="submit" size="lg" block loading={submitting}>
            {t('initialPassword.submit')}
          </AppButton>
        </AppForm>
      </div>
    </PublicFrame>
  )
}
