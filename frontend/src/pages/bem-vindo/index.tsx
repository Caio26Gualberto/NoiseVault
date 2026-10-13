import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'

import celticFrost from '@/assets/celtic-frost.jpg'
import { homePath } from '@/auth/routes'
import { useAuth } from '@/auth/use-auth'
import { markWelcomeSeen } from '@/auth/welcome-storage'
import { AppButton } from '@/components/app-button'

export function BemVindoPage() {
  const { t } = useTranslation('auth')
  const { user } = useAuth()
  const navigate = useNavigate()
  const paragraphs = t('welcome.body', { returnObjects: true }) as string[]

  const start = () => {
    if (user) markWelcomeSeen(user.id)
    navigate(homePath, { replace: true })
  }

  return (
    <div className="dark relative flex min-h-dvh items-center justify-center overflow-hidden px-4 py-6 text-foreground">
      <img
        src={celticFrost}
        alt={t('welcome.imageAlt')}
        className="fixed inset-0 -z-20 size-full object-cover brightness-[0.7] contrast-[1.1]"
      />
      <div aria-hidden className="fixed inset-0 -z-10 bg-scrim/40" />

      <main className="app-page-enter w-full max-w-[880px] rounded-xl border border-on-scrim/10 bg-scrim/60 p-5 text-center text-on-scrim backdrop-blur-md sm:px-8 sm:py-7">
        <h1 className="app-h1 mb-5 text-balance text-primary">{t('welcome.title')}</h1>

        <div className="mb-6 flex flex-col gap-3 text-left text-[15px] leading-relaxed text-on-scrim/95 md:text-justify md:leading-[1.65]">
          {paragraphs.map((paragraph) => (
            <p key={paragraph}>{paragraph}</p>
          ))}
        </div>

        <AppButton size="lg" onClick={start} className="min-w-[220px]">
          {t('welcome.start')}
        </AppButton>
      </main>
    </div>
  )
}
