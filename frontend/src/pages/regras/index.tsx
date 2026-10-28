import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'

import celticFrost from '@/assets/celtic-frost.jpg'
import { homePath, loginPath } from '@/auth/routes'
import { hasAcceptedRules, markRulesAccepted } from '@/auth/rules-storage'
import { useAuth } from '@/auth/use-auth'
import { AppButton } from '@/components/app-button'

type RuleItem = { title: string; body: string }

export function RegrasPage() {
  const { t } = useTranslation('institucional')
  const { status, user } = useAuth()
  const navigate = useNavigate()
  const items = t('rules.items', { returnObjects: true }) as RuleItem[]
  const signedIn = status === 'authenticated' && user !== null
  const pending = signedIn && !hasAcceptedRules(user.id)

  const proceed = () => {
    if (!signedIn) {
      navigate(loginPath)
      return
    }
    if (pending) markRulesAccepted(user.id)
    navigate(homePath, { replace: pending })
  }

  const label = !signedIn ? t('rules.signIn') : pending ? t('rules.accept') : t('rules.back')

  return (
    <div className="dark relative flex min-h-dvh items-center justify-center overflow-hidden px-4 pt-[calc(1.5rem+env(safe-area-inset-top))] pb-[calc(1.5rem+env(safe-area-inset-bottom))] text-foreground">
      <img
        src={celticFrost}
        alt={t('rules.imageAlt')}
        className="fixed inset-0 -z-20 size-full object-cover brightness-[0.7] contrast-[1.1]"
      />
      <div aria-hidden className="fixed inset-0 -z-10 bg-scrim/40" />

      <main className="app-page-enter w-full max-w-[720px] rounded-xl border border-on-scrim/10 bg-scrim/60 p-5 text-on-scrim backdrop-blur-md sm:px-8 sm:py-7">
        <h1 className="app-h1 mb-4 text-balance text-center text-primary">{t('rules.title')}</h1>

        <p className="mb-5 text-[15px] leading-relaxed text-on-scrim/95">{t('rules.intro')}</p>

        <ol className="mb-5 flex flex-col gap-4">
          {items.map((item, index) => (
            <li key={item.title} className="flex gap-3">
              <span className="font-heading text-xl leading-none text-primary tabular-nums">{index + 1}</span>
              <div className="flex flex-col gap-1">
                <h2 className="app-h3">{item.title}</h2>
                <p className="text-[15px] leading-relaxed text-on-scrim/90">{item.body}</p>
              </div>
            </li>
          ))}
        </ol>

        <p className="app-meta mb-6 text-on-scrim/75">{t('rules.closing')}</p>

        <div className="flex justify-center">
          <AppButton size="lg" onClick={proceed} className="min-w-[220px]">
            {label}
          </AppButton>
        </div>
      </main>
    </div>
  )
}
