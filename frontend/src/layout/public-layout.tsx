import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Outlet } from 'react-router-dom'

import { loginPath } from '../auth/routes'
import { Wordmark } from './wordmark'

export function PublicFrame({ children }: { children: ReactNode }) {
  const { t } = useTranslation()

  return (
    <div className="relative flex min-h-dvh flex-col items-center justify-center overflow-hidden px-4 py-10">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-x-0 top-0 h-[40vh] bg-[radial-gradient(ellipse_at_top,_var(--primary)_0%,_transparent_60%)] opacity-20"
      />
      <div className="relative flex w-full max-w-[400px] flex-col items-center gap-6">
        <div className="flex flex-col items-center gap-1 text-center">
          <Wordmark size="lg" to={loginPath} />
          <p className="app-meta">{t('tagline')}</p>
        </div>
        <div className="app-page-enter w-full rounded-xl border bg-card p-6 md:p-8">{children}</div>
      </div>
    </div>
  )
}

export function PublicLayout() {
  return (
    <PublicFrame>
      <Outlet />
    </PublicFrame>
  )
}
