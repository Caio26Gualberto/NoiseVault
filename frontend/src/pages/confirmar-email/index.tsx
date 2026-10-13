import { zodResolver } from '@hookform/resolvers/zod'
import { CircleCheckIcon, CircleXIcon, Loader2Icon } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router-dom'
import { z } from 'zod'

import { ApiCodes } from '@/api/api-codes'
import { feedback } from '@/api/feedback-sink'
import { ApiError } from '@/api/types'
import { confirmEmail, resendConfirmation } from '@/auth/auth-service'
import { loginPath } from '@/auth/routes'
import { emailSchema } from '@/auth/validators'
import { AppButton } from '@/components/app-button'
import { AppField, AppForm } from '@/components/app-field'

type Status = 'missing' | 'working' | 'confirmed' | 'already' | 'invalid'

const resendSchema = z.object({ email: emailSchema })

export function ConfirmarEmailPage() {
  const { t } = useTranslation('auth')
  const [params] = useSearchParams()
  const userId = Number(params.get('userId'))
  const token = params.get('token') ?? ''
  const hasLink = Number.isInteger(userId) && userId > 0 && token.length > 0
  const [status, setStatus] = useState<Status>(hasLink ? 'working' : 'missing')
  const [resending, setResending] = useState(false)
  const started = useRef(false)
  const form = useForm<z.infer<typeof resendSchema>>({
    resolver: zodResolver(resendSchema),
    defaultValues: { email: '' },
  })

  useEffect(() => {
    if (!hasLink || started.current) return
    started.current = true

    confirmEmail(userId, token)
      .then((result) => setStatus(result.alreadyConfirmed ? 'already' : 'confirmed'))
      .catch((error: unknown) => {
        if (error instanceof ApiError && error.code !== ApiCodes.emailConfirmationInvalid) {
          feedback.error(error.code, error.backendMessage)
        }
        setStatus('invalid')
      })
  }, [hasLink, token, userId])

  const resend = async ({ email }: { email: string }) => {
    setResending(true)
    try {
      await resendConfirmation(email)
    } catch {
      return
    } finally {
      setResending(false)
    }
  }

  if (status === 'working') {
    return (
      <div className="flex flex-col items-center gap-4 py-6 text-center" role="status">
        <Loader2Icon className="size-8 animate-spin text-primary" />
        <h1 className="app-h2">{t('confirm.title')}</h1>
        <p className="text-sm text-muted-foreground">{t('confirm.working')}</p>
      </div>
    )
  }

  if (status === 'confirmed' || status === 'already') {
    const prefix = status === 'confirmed' ? 'success' : 'already'
    return (
      <div className="flex flex-col items-center gap-5 text-center" role="status">
        <div className="flex size-14 items-center justify-center rounded-full bg-success/15 text-success">
          <CircleCheckIcon className="size-7" />
        </div>
        <div className="flex flex-col gap-1">
          <h1 className="app-h2">{t(`confirm.${prefix}Title`)}</h1>
          <p className="text-sm text-muted-foreground">{t(`confirm.${prefix}Description`)}</p>
        </div>
        <AppButton size="lg" block asChild>
          <Link to={loginPath}>{t('confirm.login')}</Link>
        </AppButton>
      </div>
    )
  }

  const prefix = status === 'missing' ? 'missing' : 'invalid'

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col items-center gap-4 text-center">
        <div className="flex size-14 items-center justify-center rounded-full bg-destructive/15 text-destructive">
          <CircleXIcon className="size-7" />
        </div>
        <div className="flex flex-col gap-1">
          <h1 className="app-h2">{t(`confirm.${prefix}Title`)}</h1>
          <p className="text-sm text-muted-foreground">{t(`confirm.${prefix}Description`)}</p>
        </div>
      </div>
      <AppForm form={form} onSubmit={resend}>
        <AppField
          name="email"
          label={t('fields.email')}
          type="email"
          autoComplete="email"
          inputMode="email"
          placeholder={t('fields.emailPlaceholder')}
        />
        <AppButton type="submit" intent="neutral" size="lg" block loading={resending}>
          {t('confirm.resend')}
        </AppButton>
      </AppForm>
      <Link to={loginPath} className="text-center text-sm font-medium text-primary hover:underline">
        {t('confirm.login')}
      </Link>
    </div>
  )
}
