import { useTranslation } from 'react-i18next'
import { NavLink, useLocation } from 'react-router-dom'

import { cn } from '@/lib/utils'

import { useAuth } from '../auth/use-auth'
import { useCan } from '../auth/use-can'
import { resolveActivePath } from './nav-active'
import { navSections } from './nav-config'

export function BottomNav() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const can = useCan()
  const location = useLocation()
  const activePath = resolveActivePath(location.pathname)
  const nickname = user?.nickname ?? ''

  const items = navSections
    .flatMap((section) =>
      section.items.map((item) => ({ item, permission: item.permission ?? section.permission })),
    )
    .filter(({ item, permission }) => item.mobile && (permission ? can(permission) : true))
    .map(({ item }) => item)
    .slice(0, 5)

  return (
    <nav
      className="fixed inset-x-0 bottom-0 z-40 flex border-t bg-background/95 pb-[env(safe-area-inset-bottom)] backdrop-blur md:hidden"
      aria-label={t('layout:nav.community')}
    >
      {items.map((item) => {
        const Icon = item.icon
        const active = item.path === activePath
        return (
          <NavLink
            key={item.path}
            to={item.mobilePath ? item.mobilePath(nickname) : item.path}
            end
            aria-current={active ? 'page' : undefined}
            className={cn(
              'flex min-w-0 flex-1 flex-col items-center gap-1 px-1 py-2 text-[11px] font-medium text-muted-foreground transition-colors focus-visible:outline-none',
              active && 'text-primary',
            )}
          >
            <Icon className="size-5" />
            <span className="truncate">{t(item.labelKey)}</span>
          </NavLink>
        )
      })}
    </nav>
  )
}
