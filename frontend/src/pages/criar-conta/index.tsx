import { zodResolver } from '@hookform/resolvers/zod'
import { MailCheckIcon } from 'lucide-react'
import { useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { z } from 'zod'

import { register, resendConfirmation } from '@/auth/auth-service'
import { loginPath } from '@/auth/routes'
import { emailSchema, nameSchema, nicknameSchema, passwordSchema, withPasswordConfirmation } from '@/auth/validators'
import { AppButton } from '@/components/app-button'
import { AppField, AppForm, AppPasswordField } from '@/components/app-field'

const schema = withPasswordConfirmation(
  z.object({
    name: nameSchema,
    nickname: nicknameSchema,
    email: emailSchema,
    password: passwordSchema,
    confirmPassword: z.string(),
  }),
)

type RegisterValues = z.infer<typeof schema>

export function CriarContaPage() {
  const { t } = useTranslation('auth')
  const [submitting, setSubmitting] = useState(false)
  const [resending, setResending] = useState(false)
  const [sentTo, setSentTo] = useState<string | null>(null)
  const form = useForm<RegisterValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: '', nickname: '', email: '', password: '', confirmPassword: '' },
  })

  const nickname = useWatch({ control: form.control, name: 'nickname' })

  const submit = async (values: RegisterValues) => {
    setSubmitting(true)
    try {
      const result = await register({
        name: values.name,
        nickname: values.nickname,
        email: values.email,
        password: values.password,
      })
      setSentTo(result.email)
    } catch {
      return
    } finally {
      setSubmitting(false)
    }
  }

  const resend = async () => {
    if (!sentTo) return
    setResending(true)
    try {
      await resendConfirmation(sentTo)
    } catch {
      return
    } finally {
      setResending(false)
    }
  }

  if (sentTo) {
    return (
      <div className="flex flex-col items-center gap-5 text-center" role="status">
        <div className="flex size-14 items-center justify-center rounded-full bg-success/15 text-success">
          <MailCheckIcon className="size-7" />
        </div>
        <div className="flex flex-col gap-1">
          <h1 className="app-h2">{t('register.sentTitle')}</h1>
          <p className="text-sm text-muted-foreground">{t('register.sentDescription', { email: sentTo })}</p>
        </div>
        <div className="flex w-full flex-col gap-2">
          <AppButton size="lg" block asChild>
            <Link to={loginPath}>{t('register.goToLogin')}</Link>
          </AppButton>
          <AppButton intent="ghost" loading={resending} onClick={() => void resend()}>
            {t('register.resend')}
          </AppButton>
        </div>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h1 className="app-h2">{t('register.title')}</h1>
        <p className="text-sm text-muted-foreground">{t('register.subtitle')}</p>
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
        <AppField
          name="email"
          label={t('fields.email')}
          type="email"
          autoComplete="email"
          inputMode="email"
          placeholder={t('fields.emailPlaceholder')}
          hint={t('fields.emailTooltip')}
        />
        <AppPasswordField name="password" label={t('fields.password')} autoComplete="new-password" />
        <AppPasswordField name="confirmPassword" label={t('fields.confirmPassword')} autoComplete="new-password" />
        <AppButton type="submit" size="lg" block loading={submitting}>
          {t('register.submit')}
        </AppButton>
      </AppForm>

      <p className="text-center text-sm text-muted-foreground">
        {t('register.hasAccount')}{' '}
        <Link to={loginPath} className="font-medium text-primary hover:underline">
          {t('register.login')}
        </Link>
      </p>
    </div>
  )
}
