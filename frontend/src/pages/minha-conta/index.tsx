import { zodResolver } from '@hookform/resolvers/zod'
import { EyeIcon, MailIcon, MailWarningIcon, PencilIcon, ShieldCheckIcon } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { z } from 'zod'

import { tokenStorage } from '@/api/token-storage'
import { changePassword, resendConfirmation } from '@/auth/auth-service'
import { profileEditPath } from '@/auth/routes'
import { useAuth } from '@/auth/use-auth'
import { passwordSchema, withPasswordConfirmation } from '@/auth/validators'
import { AppAvatar } from '@/components/app-avatar'
import { AppButton } from '@/components/app-button'
import { AppForm, AppPasswordField } from '@/components/app-field'
import { AppFormAsideBox, AppFormLayout, AppFormPanel } from '@/components/app-form-layout'
import { AppPageHeader } from '@/components/app-page-header'
import { LocationControl } from '@/pages/ensaio/components/location-control'

import { NotificationSettings } from './components/notification-settings'
import { SignInMethodsPanel } from './components/sign-in-methods'
import { useSignInMethods } from './hooks/use-sign-in-methods'
import { createPassword } from './services/conta-service'

const passwordFormSchema = withPasswordConfirmation(
  z.object({
    currentPassword: z.string().min(1, 'validation.required'),
    password: passwordSchema,
    confirmPassword: z.string(),
  }),
)

type PasswordValues = z.infer<typeof passwordFormSchema>

const createPasswordFormSchema = withPasswordConfirmation(
  z.object({
    password: passwordSchema,
    confirmPassword: z.string(),
  }),
)

type CreatePasswordValues = z.infer<typeof createPasswordFormSchema>

export function MinhaContaPage() {
  const { t } = useTranslation('conta')
  const { t: tAuth } = useTranslation('auth')
  const { user } = useAuth()
  const { methods, setMethods } = useSignInMethods()
  const [savingPassword, setSavingPassword] = useState(false)
  const [resending, setResending] = useState(false)

  const passwordForm = useForm<PasswordValues>({
    resolver: zodResolver(passwordFormSchema),
    defaultValues: { currentPassword: '', password: '', confirmPassword: '' },
  })

  const createPasswordForm = useForm<CreatePasswordValues>({
    resolver: zodResolver(createPasswordFormSchema),
    defaultValues: { password: '', confirmPassword: '' },
  })

  if (!user) return null

  const saveNewPassword = async ({ password }: CreatePasswordValues) => {
    setSavingPassword(true)
    try {
      setMethods(await createPassword(password))
      createPasswordForm.reset()
    } catch {
      return
    } finally {
      setSavingPassword(false)
    }
  }

  const resendEmail = async () => {
    setResending(true)
    try {
      await resendConfirmation(user.email)
    } catch {
      return
    } finally {
      setResending(false)
    }
  }

  const savePassword = async ({ currentPassword, password }: PasswordValues) => {
    setSavingPassword(true)
    try {
      const tokens = await changePassword(currentPassword, password)
      tokenStorage.saveToken(tokens.token)
      passwordForm.reset()
    } catch {
      return
    } finally {
      setSavingPassword(false)
    }
  }

  return (
    <div className="app-page-enter flex flex-col gap-6">
      <AppPageHeader title={t('title')} description={t('description')} />

      <AppFormLayout
        aside={
          <>
            <div className="flex items-center gap-3 rounded-xl border bg-card p-4">
              <AppAvatar name={user.name} src={user.avatarUrl} size="lg" />
              <div className="flex min-w-0 flex-col">
                <span className="truncate font-semibold">{user.name}</span>
                <span className="app-meta truncate">@{user.nickname}</span>
              </div>
            </div>
            <AppFormAsideBox title={t('account.profileTitle')} description={t('account.profileDescription')}>
              <div className="flex flex-wrap gap-2">
                <AppButton asChild size="sm">
                  <Link to={profileEditPath}>
                    <PencilIcon />
                    {t('account.editProfile')}
                  </Link>
                </AppButton>
                <AppButton asChild size="sm" intent="neutral">
                  <Link to={`/perfil/${user.nickname}`}>
                    <EyeIcon />
                    {t('account.viewProfile')}
                  </Link>
                </AppButton>
              </div>
            </AppFormAsideBox>
            <p className="flex gap-1.5 px-1 text-[13px] text-muted-foreground">
              <ShieldCheckIcon className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
              {t('account.privateHint')}
            </p>
          </>
        }
      >
        <AppFormPanel title={t('account.accessTitle')} description={t('account.accessDescription')}>
          <div className="flex flex-col gap-1.5">
            <span className="text-sm font-medium">{t('profile.email')}</span>
            <span className="flex h-11 items-center gap-2 truncate rounded-lg border bg-muted/40 px-3 text-sm text-muted-foreground md:h-10">
              <MailIcon className="size-4 shrink-0" aria-hidden="true" />
              {user.email}
            </span>
            <span className="text-[13px] text-muted-foreground">{t('account.emailHint')}</span>
          </div>
          {methods && !methods.emailConfirmed ? (
            <div role="status" className="flex flex-col gap-3 rounded-lg border border-warning/40 bg-warning/10 p-3">
              <div className="flex items-start gap-2">
                <MailWarningIcon className="mt-0.5 size-4 shrink-0 text-warning" aria-hidden="true" />
                <div className="flex flex-col gap-0.5">
                  <p className="text-sm font-medium">{t('account.emailPendingTitle')}</p>
                  <p className="text-[13px] text-muted-foreground">{t('account.emailPendingDescription')}</p>
                </div>
              </div>
              <AppButton type="button" intent="neutral" size="sm" loading={resending} onClick={() => void resendEmail()} className="self-start">
                {t('account.emailPendingResend')}
              </AppButton>
            </div>
          ) : null}
          <div className="flex flex-col gap-1.5">
            <span className="text-sm font-medium">{t('profile.nickname')}</span>
            <span className="flex h-11 items-center rounded-lg border bg-muted/40 px-3 text-sm text-muted-foreground md:h-10">@{user.nickname}</span>
            <span className="text-[13px] text-muted-foreground">{t('profile.nicknameHint', { nickname: user.nickname })}</span>
          </div>
        </AppFormPanel>

        {methods?.hasPassword === false ? (
          <AppFormPanel title={t('password.createTitle')} description={t('password.createDescription')}>
            <AppForm form={createPasswordForm} onSubmit={saveNewPassword}>
              <AppPasswordField name="password" label={tAuth('fields.newPassword')} autoComplete="new-password" />
              <AppPasswordField name="confirmPassword" label={tAuth('fields.confirmPassword')} autoComplete="new-password" />
              <AppButton type="submit" intent="secondary" loading={savingPassword} className="self-start">
                {t('password.create')}
              </AppButton>
            </AppForm>
          </AppFormPanel>
        ) : (
          <AppFormPanel title={t('password.title')} description={t('password.description')}>
            <AppForm form={passwordForm} onSubmit={savePassword}>
              <AppPasswordField name="currentPassword" label={tAuth('fields.currentPassword')} autoComplete="current-password" />
              <AppPasswordField name="password" label={tAuth('fields.newPassword')} autoComplete="new-password" />
              <AppPasswordField name="confirmPassword" label={tAuth('fields.confirmPassword')} autoComplete="new-password" />
              <AppButton type="submit" intent="secondary" loading={savingPassword} className="self-start">
                {t('password.save')}
              </AppButton>
            </AppForm>
          </AppFormPanel>
        )}

        <SignInMethodsPanel methods={methods} onChange={setMethods} />

        <NotificationSettings />

        <AppFormPanel title={t('account.locationTitle')} description={t('account.locationDescription')}>
          <LocationControl />
        </AppFormPanel>
      </AppFormLayout>
    </div>
  )
}
