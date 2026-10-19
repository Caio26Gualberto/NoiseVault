import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, Outlet } from 'react-router-dom'

import { dataDeletionPath, homePath, loginPath, privacyPath } from '../auth/routes'
import { useAuth } from '../auth/use-auth'
import { cn } from '@/lib/utils'
import { Wordmark } from './wordmark'

export function PublicFrame({ children, wide = false }: { children: ReactNode; wide?: boolean }) {
  const { t } = useTranslation()
  const { status } = useAuth()
  const signedIn = status === 'authenticated'
  const homeLink = signedIn ? homePath : loginPath

  return (
    <div className="relative flex min-h-dvh flex-col items-center justify-center overflow-hidden px-4 pt-[calc(2.5rem+env(safe-area-inset-top))] pb-[calc(2.5rem+env(safe-area-inset-bottom))]">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-x-0 top-0 h-[40vh] bg-[radial-gradient(ellipse_at_top,_var(--primary)_0%,_transparent_60%)] opacity-20"
      />
      <div className={cn('relative flex w-full flex-col items-center gap-6', wide ? 'max-w-[720px]' : 'max-w-[400px]')}>
        <div className="flex flex-col items-center gap-1 text-center">
          <Wordmark size="lg" to={homeLink} />
          <p className="app-meta">{t('tagline')}</p>
        </div>
        <div className="app-page-enter w-full rounded-xl border bg-card p-6 md:p-8">{children}</div>
        <nav aria-label={t('legal.label')} className="flex flex-wrap justify-center gap-x-4 gap-y-1 text-[13px] text-muted-foreground">
          {signedIn || wide ? (
            <Link to={homeLink} className="font-medium text-primary hover:underline">
              {t(signedIn ? 'legal.backHome' : 'legal.signIn')}
            </Link>
          ) : null}
          <Link to={privacyPath} className="hover:text-foreground hover:underline">
            {t('legal.privacy')}
          </Link>
          <Link to={dataDeletionPath} className="hover:text-foreground hover:underline">
            {t('legal.dataDeletion')}
          </Link>
        </nav>
      </div>
    </div>
  )
}

export function PublicLayout({ wide = false }: { wide?: boolean }) {
  return (
    <PublicFrame wide={wide}>
      <Outlet />
    </PublicFrame>
  )
}
