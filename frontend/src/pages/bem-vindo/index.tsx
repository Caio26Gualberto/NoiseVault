import { CheckIcon } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'

import { completeOnboarding } from '@/auth/auth-service'
import { homePath } from '@/auth/routes'
import { useAuth } from '@/auth/use-auth'
import { AppButton } from '@/components/app-button'
import { AppSkeleton } from '@/components/app-skeleton'
import { PublicFrame } from '@/layout/public-layout'
import { cn } from '@/lib/utils'

import { getAvatarOptions } from './services/onboarding-service'
import type { AvatarOptionDto } from './services/onboarding-service'

const totalSteps = 2

export function BemVindoPage() {
  const { t } = useTranslation('auth')
  const { user, refreshUser } = useAuth()
  const navigate = useNavigate()
  const [step, setStep] = useState(1)
  const [options, setOptions] = useState<AvatarOptionDto[] | null>(null)
  const [selected, setSelected] = useState<string | null>(null)
  const [finishing, setFinishing] = useState(false)

  useEffect(() => {
    if (step !== 2 || options) return
    let cancelled = false
    getAvatarOptions().then((result) => {
      if (!cancelled) setOptions(result)
    })
    return () => {
      cancelled = true
    }
  }, [step, options])

  const finish = async () => {
    setFinishing(true)
    try {
      await completeOnboarding()
      await refreshUser()
      navigate(homePath, { replace: true })
    } catch {
      return
    } finally {
      setFinishing(false)
    }
  }

  const firstName = user?.name.split(' ')[0] ?? ''

  return (
    <PublicFrame>
      <div className="flex flex-col gap-6">
        <p className="app-meta">{t('onboarding.step', { current: step, total: totalSteps })}</p>

        {step === 1 ? (
          <>
            <div className="flex flex-col gap-2">
              <h1 className="app-h2">{t('onboarding.welcomeTitle', { name: firstName })}</h1>
              <p className="text-sm text-muted-foreground">{t('onboarding.welcomeDescription')}</p>
            </div>
            <AppButton size="lg" block onClick={() => setStep(2)}>
              {t('onboarding.welcomeNext')}
            </AppButton>
          </>
        ) : (
          <>
            <div className="flex flex-col gap-2">
              <h1 className="app-h2">{t('onboarding.avatarTitle')}</h1>
              <p className="text-sm text-muted-foreground">{t('onboarding.avatarDescription')}</p>
            </div>

            {options ? (
              <div className="grid grid-cols-4 gap-3" role="radiogroup" aria-label={t('onboarding.avatarTitle')}>
                {options.map((option) => {
                  const active = option.id === selected
                  return (
                    <button
                      key={option.id}
                      type="button"
                      role="radio"
                      aria-checked={active}
                      onClick={() => setSelected(option.id)}
                      className={cn(
                        'relative aspect-square overflow-hidden rounded-full border-2 border-transparent bg-muted transition-all focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none',
                        active ? 'border-primary' : 'hover:border-border',
                      )}
                    >
                      <img src={option.url} alt="" className="size-full object-cover" />
                      {active ? (
                        <span className="absolute inset-0 flex items-center justify-center bg-primary/40 text-primary-foreground">
                          <CheckIcon className="size-6" />
                        </span>
                      ) : null}
                    </button>
                  )
                })}
              </div>
            ) : (
              <AppSkeleton preset="lines" count={4} />
            )}

            <div className="flex flex-col gap-2">
              <AppButton size="lg" block loading={finishing} onClick={() => void finish()}>
                {t('onboarding.finish')}
              </AppButton>
              <AppButton intent="ghost" disabled={finishing} onClick={() => void finish()}>
                {t('onboarding.skip')}
              </AppButton>
            </div>
          </>
        )}
      </div>
    </PublicFrame>
  )
}
