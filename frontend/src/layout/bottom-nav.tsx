import { LogInIcon, MenuIcon } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { NavLink, useLocation } from 'react-router-dom'

import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/components/ui/sheet'
import { cn } from '@/lib/utils'

import { loginPath } from '../auth/routes'
import { useAuth } from '../auth/use-auth'
import { useCan } from '../auth/use-can'
import { useIsVisitor } from '../auth/use-require-login'
import { resolveActivePath } from './nav-active'
import { navSections } from './nav-config'
import type { NavLeaf } from './nav-config'

const itemClass =
  'flex min-w-0 flex-1 items-center justify-center px-1 py-3 text-muted-foreground transition-colors focus-visible:outline-none focus-visible:text-foreground'

export function BottomNav() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const can = useCan()
  const visitor = useIsVisitor()
  const location = useLocation()
  const [open, setOpen] = useState(false)
  const activePath = resolveActivePath(location.pathname)
  const nickname = user?.nickname ?? ''

  const visible = navSections
    .map((section) => ({
      ...section,
      items: section.items.filter((item) => {
        if (visitor) return item.public === true
        const permission = item.permission ?? section.permission
        return permission ? can(permission) : true
      }),
    }))
    .filter((section) => section.items.length > 0)

  const leaves = visible.flatMap((section) => section.items)
  const primary = leaves.filter((item) => item.mobile).slice(0, 4)
  const rest = visible
    .map((section) => ({ ...section, items: section.items.filter((item) => !primary.includes(item)) }))
    .filter((section) => section.items.length > 0)
  const restActive = rest.some((section) => section.items.some((item) => item.path === activePath))

  const pathOf = (item: NavLeaf) => (item.mobilePath ? item.mobilePath(nickname) : item.path)

  return (
    <nav
      className="fixed inset-x-0 bottom-0 z-40 flex border-t bg-background/95 pb-[env(safe-area-inset-bottom)] backdrop-blur md:hidden"
      aria-label={t('layout:nav.community')}
    >
      {primary.map((item) => {
        const Icon = item.icon
        const active = item.path === activePath
        return (
          <NavLink
            key={item.path}
            to={pathOf(item)}
            end
            aria-current={active ? 'page' : undefined}
            className={cn(itemClass, active && 'text-primary')}
          >
            <Icon className="size-6" />
            <span className="sr-only">{t(item.labelKey)}</span>
          </NavLink>
        )
      })}

      {visitor ? (
        <NavLink to={loginPath} state={{ from: `${location.pathname}${location.search}` }} className={itemClass}>
          <LogInIcon className="size-6" />
          <span className="sr-only">{t('layout:visitor.signIn')}</span>
        </NavLink>
      ) : (
        <button type="button" onClick={() => setOpen(true)} className={cn(itemClass, restActive && 'text-primary')} aria-haspopup="dialog">
          <MenuIcon className="size-6" />
          <span className="sr-only">{t('layout:nav.more')}</span>
        </button>
      )}

      <Sheet open={open} onOpenChange={setOpen}>
        <SheetContent side="bottom" className="rounded-t-xl pb-[env(safe-area-inset-bottom)]">
          <SheetHeader>
            <SheetTitle className="app-h3">{t('layout:nav.moreSheetTitle')}</SheetTitle>
          </SheetHeader>
          <div className="flex flex-col gap-4 px-4 pb-4">
            {rest.map((section) => (
              <div key={section.id} className="flex flex-col gap-1">
                <p className="text-[11px] font-semibold tracking-[0.12em] text-muted-foreground uppercase">{t(section.labelKey)}</p>
                {section.items.map((item) => {
                  const Icon = item.icon
                  const active = item.path === activePath
                  return (
                    <NavLink
                      key={item.path}
                      to={pathOf(item)}
                      end
                      onClick={() => setOpen(false)}
                      className={cn(
                        'flex items-center gap-3 rounded-lg px-2 py-2.5 text-[15px] font-medium transition-colors hover:bg-accent',
                        active && 'bg-accent text-primary',
                      )}
                    >
                      <Icon className="size-5" />
                      {t(item.labelKey)}
                    </NavLink>
                  )
                })}
              </div>
            ))}
          </div>
        </SheetContent>
      </Sheet>
    </nav>
  )
}
