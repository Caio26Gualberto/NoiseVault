import { zodResolver } from '@hookform/resolvers/zod'
import { MailWarningIcon } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { z } from 'zod'

import { emailSchema } from '@/auth/validators'
import { forgotPasswordPath, registerPath } from '@/auth/routes'
import { AppButton } from '@/components/app-button'
import { AppField, AppForm, AppPasswordField } from '@/components/app-field'

import { useLogin } from './hooks/use-login'

const schema = z.object({
  email: emailSchema,
  password: z.string().min(1, 'validation.required'),
})

type LoginValues = z.infer<typeof schema>

export function EntrarPage() {
  const { t } = useTranslation('auth')
  const { submit, submitting, pendingEmail, resend, resending } = useLogin()
  const form = useForm<LoginValues>({
    resolver: zodResolver(schema),
    defaultValues: { email: '', password: '' },
  })

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h1 className="app-h2">{t('login.title')}</h1>
        <p className="text-sm text-muted-foreground">{t('login.subtitle')}</p>
      </div>

      {pendingEmail ? (
        <div role="status" className="flex flex-col gap-3 rounded-lg border border-warning/40 bg-warning/10 p-4">
          <div className="flex items-start gap-3">
            <MailWarningIcon className="mt-0.5 size-5 shrink-0 text-warning" />
            <div className="flex flex-col gap-1">
              <p className="text-sm font-medium">{t('login.pendingTitle')}</p>
              <p className="text-[13px] text-muted-foreground">{t('login.pendingDescription', { email: pendingEmail })}</p>
            </div>
          </div>
          <AppButton intent="neutral" size="sm" loading={resending} onClick={() => void resend()} className="self-start">
            {t('login.resend')}
          </AppButton>
        </div>
      ) : null}

      <AppForm form={form} onSubmit={(values) => submit(values.email, values.password)}>
        <AppField
          name="email"
          label={t('fields.email')}
          type="email"
          autoComplete="email"
          inputMode="email"
          placeholder={t('fields.emailPlaceholder')}
        />
        <div className="flex flex-col gap-2">
          <AppPasswordField name="password" label={t('fields.password')} autoComplete="current-password" />
          <Link to={forgotPasswordPath} className="self-end text-[13px] text-primary hover:underline">
            {t('login.forgot')}
          </Link>
        </div>
        <AppButton type="submit" size="lg" block loading={submitting}>
          {t('login.submit')}
        </AppButton>
      </AppForm>

      <p className="text-center text-sm text-muted-foreground">
        {t('login.noAccount')}{' '}
        <Link to={registerPath} className="font-medium text-primary hover:underline">
          {t('login.register')}
        </Link>
      </p>
    </div>
  )
}
