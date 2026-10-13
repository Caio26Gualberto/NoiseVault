import { zodResolver } from '@hookform/resolvers/zod'
import { CircleXIcon } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { z } from 'zod'

import { resetPassword } from '@/auth/auth-service'
import { forgotPasswordPath, loginPath } from '@/auth/routes'
import { passwordSchema, withPasswordConfirmation } from '@/auth/validators'
import { AppButton } from '@/components/app-button'
import { AppForm, AppPasswordField } from '@/components/app-field'

const schema = withPasswordConfirmation(
  z.object({
    password: passwordSchema,
    confirmPassword: z.string(),
  }),
)

type ResetValues = z.infer<typeof schema>

export function RedefinirSenhaPage() {
  const { t } = useTranslation('auth')
  const [params] = useSearchParams()
  const navigate = useNavigate()
  const email = params.get('email') ?? ''
  const token = params.get('token') ?? ''
  const hasLink = email.length > 0 && token.length > 0
  const [submitting, setSubmitting] = useState(false)
  const form = useForm<ResetValues>({
    resolver: zodResolver(schema),
    defaultValues: { password: '', confirmPassword: '' },
  })

  const submit = async ({ password }: ResetValues) => {
    setSubmitting(true)
    try {
      await resetPassword(email, token, password)
      navigate(loginPath, { replace: true })
    } catch {
      return
    } finally {
      setSubmitting(false)
    }
  }

  if (!hasLink) {
    return (
      <div className="flex flex-col items-center gap-5 text-center">
        <div className="flex size-14 items-center justify-center rounded-full bg-destructive/15 text-destructive">
          <CircleXIcon className="size-7" />
        </div>
        <div className="flex flex-col gap-1">
          <h1 className="app-h2">{t('reset.missingTitle')}</h1>
          <p className="text-sm text-muted-foreground">{t('reset.missingDescription')}</p>
        </div>
        <AppButton intent="neutral" size="lg" block asChild>
          <Link to={forgotPasswordPath}>{t('reset.requestAgain')}</Link>
        </AppButton>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h1 className="app-h2">{t('reset.title')}</h1>
        <p className="text-sm text-muted-foreground">{t('reset.subtitle', { email })}</p>
      </div>
      <AppForm form={form} onSubmit={submit}>
        <AppPasswordField name="password" label={t('fields.newPassword')} autoComplete="new-password" />
        <AppPasswordField name="confirmPassword" label={t('fields.confirmPassword')} autoComplete="new-password" />
        <AppButton type="submit" size="lg" block loading={submitting}>
          {t('reset.submit')}
        </AppButton>
      </AppForm>
    </div>
  )
}
