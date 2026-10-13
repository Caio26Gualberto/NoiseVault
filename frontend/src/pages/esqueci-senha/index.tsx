import { zodResolver } from '@hookform/resolvers/zod'
import { MailCheckIcon } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { z } from 'zod'

import { forgotPassword } from '@/auth/auth-service'
import { loginPath } from '@/auth/routes'
import { emailSchema } from '@/auth/validators'
import { AppButton } from '@/components/app-button'
import { AppField, AppForm } from '@/components/app-field'

const schema = z.object({ email: emailSchema })

type ForgotValues = z.infer<typeof schema>

export function EsqueciSenhaPage() {
  const { t } = useTranslation('auth')
  const [submitting, setSubmitting] = useState(false)
  const [sentTo, setSentTo] = useState<string | null>(null)
  const form = useForm<ForgotValues>({ resolver: zodResolver(schema), defaultValues: { email: '' } })

  const submit = async ({ email }: ForgotValues) => {
    setSubmitting(true)
    try {
      await forgotPassword(email)
      setSentTo(email)
    } catch {
      return
    } finally {
      setSubmitting(false)
    }
  }

  if (sentTo) {
    return (
      <div className="flex flex-col items-center gap-5 text-center" role="status">
        <div className="flex size-14 items-center justify-center rounded-full bg-success/15 text-success">
          <MailCheckIcon className="size-7" />
        </div>
        <div className="flex flex-col gap-1">
          <h1 className="app-h2">{t('forgot.sentTitle')}</h1>
          <p className="text-sm text-muted-foreground">{t('forgot.sentDescription', { email: sentTo })}</p>
        </div>
        <AppButton intent="neutral" size="lg" block asChild>
          <Link to={loginPath}>{t('forgot.backToLogin')}</Link>
        </AppButton>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h1 className="app-h2">{t('forgot.title')}</h1>
        <p className="text-sm text-muted-foreground">{t('forgot.subtitle')}</p>
      </div>
      <AppForm form={form} onSubmit={submit}>
        <AppField
          name="email"
          label={t('fields.email')}
          type="email"
          autoComplete="email"
          inputMode="email"
          placeholder={t('fields.emailPlaceholder')}
        />
        <AppButton type="submit" size="lg" block loading={submitting}>
          {t('forgot.submit')}
        </AppButton>
      </AppForm>
      <Link to={loginPath} className="text-center text-sm font-medium text-primary hover:underline">
        {t('forgot.backToLogin')}
      </Link>
    </div>
  )
}
