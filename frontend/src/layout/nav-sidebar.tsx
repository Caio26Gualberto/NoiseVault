import { useTranslation } from 'react-i18next'
import { NavLink, useLocation } from 'react-router-dom'

import { cn } from '@/lib/utils'

import { useAuth } from '../auth/use-auth'
import { useCan } from '../auth/use-can'
import { useIsVisitor } from '../auth/use-require-login'
import { resolveActivePath } from './nav-active'
import { navSections } from './nav-config'
import type { NavLeaf } from './nav-config'
import { UserMenu } from './user-menu'
import { VisitorActions } from './visitor-actions'
import { Wordmark } from './wordmark'

function leafPath(item: NavLeaf, nickname: string): string {
  return item.mobilePath ? item.mobilePath(nickname) : item.path
}

export function NavSidebar() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const can = useCan()
  const visitor = useIsVisitor()
  const location = useLocation()
  const activePath = resolveActivePath(location.pathname)
  const nickname = user?.nickname ?? ''

  const sections = navSections
    .map((section) => ({
      ...section,
      items: section.items.filter((item) => {
        if (visitor) return item.public === true
        const permission = item.permission ?? section.permission
        return permission ? can(permission) : true
      }),
    }))
    .filter((section) => section.items.length > 0)

  return (
    <aside className="sticky top-0 hidden h-dvh w-60 shrink-0 flex-col border-r bg-sidebar px-3 py-4 md:flex">
      <div className="px-2 pb-6">
        <Wordmark />
      </div>

      <nav className="flex flex-1 flex-col gap-5 overflow-y-auto" aria-label={t('layout:nav.community')}>
        {sections.map((section) => (
          <div key={section.id} className="flex flex-col gap-0.5">
            <p className="px-2 pb-1 text-[11px] font-semibold tracking-[0.12em] text-muted-foreground uppercase">
              {t(section.labelKey)}
            </p>
            {section.items.map((item) => {
              const Icon = item.icon
              const active = item.path === activePath
              return (
                <NavLink
                  key={item.path}
                  to={leafPath(item, nickname)}
                  end
                  aria-current={active ? 'page' : undefined}
                  className={cn(
                    'flex items-center gap-3 rounded-lg px-2 py-2 text-[15px] font-medium text-sidebar-foreground/80 transition-colors hover:bg-sidebar-accent hover:text-sidebar-foreground focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none',
                    active && 'bg-sidebar-accent text-sidebar-foreground shadow-[inset_3px_0_0_0_var(--primary)]',
                  )}
                >
                  <Icon className={cn('size-5', active && 'text-primary')} />
                  <span>{t(item.labelKey)}</span>
                </NavLink>
              )
            })}
          </div>
        ))}
      </nav>

      <div className="border-t pt-3">
        {visitor ? <VisitorActions stacked /> : <UserMenu />}
      </div>
    </aside>
  )
}
