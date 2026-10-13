import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'

import { tokenStorage } from '@/api/token-storage'
import { changePassword } from '@/auth/auth-service'
import { useAuth } from '@/auth/use-auth'
import { nameSchema, passwordSchema, withPasswordConfirmation } from '@/auth/validators'
import { AppAvatar } from '@/components/app-avatar'
import { AppBadge } from '@/components/app-badge'
import { AppButton } from '@/components/app-button'
import { AppCard } from '@/components/app-card'
import { AppField, AppForm, AppPasswordField } from '@/components/app-field'
import { AppPageHeader } from '@/components/app-page-header'

import { updateName } from './services/conta-service'

const nameFormSchema = z.object({ name: nameSchema })

const passwordFormSchema = withPasswordConfirmation(
  z.object({
    currentPassword: z.string().min(1, 'validation.required'),
    password: passwordSchema,
    confirmPassword: z.string(),
  }),
)

type NameValues = z.infer<typeof nameFormSchema>
type PasswordValues = z.infer<typeof passwordFormSchema>

export function MinhaContaPage() {
  const { t } = useTranslation('conta')
  const { t: tAuth } = useTranslation('auth')
  const { user, refreshUser } = useAuth()
  const [savingName, setSavingName] = useState(false)
  const [savingPassword, setSavingPassword] = useState(false)

  const nameForm = useForm<NameValues>({
    resolver: zodResolver(nameFormSchema),
    defaultValues: { name: user?.name ?? '' },
  })

  const passwordForm = useForm<PasswordValues>({
    resolver: zodResolver(passwordFormSchema),
    defaultValues: { currentPassword: '', password: '', confirmPassword: '' },
  })

  if (!user) return null

  const saveName = async ({ name }: NameValues) => {
    setSavingName(true)
    try {
      await updateName(name)
      await refreshUser()
    } catch {
      return
    } finally {
      setSavingName(false)
    }
  }

  const savePassword = async ({ currentPassword, password }: PasswordValues) => {
    setSavingPassword(true)
    try {
      const tokens = await changePassword(currentPassword, password)
      tokenStorage.saveTokens(tokens.token, tokens.refreshToken)
      passwordForm.reset()
    } catch {
      return
    } finally {
      setSavingPassword(false)
    }
  }

  const mockRows = [
    ['avatar', 'instrument'],
    ['banner', 'city'],
    ['genres'],
  ] as const

  return (
    <div className="app-page-enter mx-auto flex max-w-2xl flex-col gap-6">
      <AppPageHeader title={t('title')} description={t('description')} />

      <AppCard title={t('profile.title')}>
        <div className="mb-5 flex items-center gap-4">
          <AppAvatar name={user.name} size="xl" />
          <div className="flex min-w-0 flex-col">
            <span className="truncate font-medium">{user.name}</span>
            <span className="app-meta truncate">@{user.nickname}</span>
          </div>
        </div>
        <AppForm form={nameForm} onSubmit={saveName}>
          <AppField name="name" label={t('profile.name')} autoComplete="name" />
          <div className="grid gap-4 md:grid-cols-2">
            <div className="flex flex-col gap-1.5">
              <span className="text-sm font-medium">{t('profile.nickname')}</span>
              <span className="flex h-11 items-center rounded-lg border bg-muted/40 px-3 text-sm text-muted-foreground md:h-10">
                @{user.nickname}
              </span>
              <span className="text-[13px] text-muted-foreground">{t('profile.nicknameHint', { nickname: user.nickname })}</span>
            </div>
            <div className="flex flex-col gap-1.5">
              <span className="text-sm font-medium">{t('profile.email')}</span>
              <span className="flex h-11 items-center rounded-lg border bg-muted/40 px-3 text-sm text-muted-foreground md:h-10">
                {user.email}
              </span>
            </div>
          </div>
          <AppButton type="submit" loading={savingName} className="self-start">
            {t('profile.save')}
          </AppButton>
        </AppForm>
      </AppCard>

      <AppCard title={t('password.title')} description={t('password.description')}>
        <AppForm form={passwordForm} onSubmit={savePassword}>
          <AppPasswordField name="currentPassword" label={tAuth('fields.currentPassword')} autoComplete="current-password" />
          <div className="grid gap-4 md:grid-cols-2">
            <AppPasswordField name="password" label={tAuth('fields.newPassword')} autoComplete="new-password" />
            <AppPasswordField name="confirmPassword" label={tAuth('fields.confirmPassword')} autoComplete="new-password" />
          </div>
          <AppButton type="submit" intent="secondary" loading={savingPassword} className="self-start">
            {t('password.save')}
          </AppButton>
        </AppForm>
      </AppCard>

      <AppCard title={t('mock.title')} description={t('mock.description')}>
        <dl className="grid gap-3 md:grid-cols-2">
          {mockRows.flat().map((key) => (
            <div key={key} className="flex items-center justify-between gap-3 rounded-lg border border-dashed px-3 py-2">
              <dt className="text-sm">{t(`mock.${key}`)}</dt>
              <dd>
                <AppBadge tone="neutral">{t('mock.soon')}</AppBadge>
              </dd>
            </div>
          ))}
        </dl>
      </AppCard>
    </div>
  )
}
