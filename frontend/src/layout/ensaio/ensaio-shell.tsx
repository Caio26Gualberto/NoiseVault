import { HouseIcon, LogOutIcon, MegaphoneIcon, PlusIcon, UserRoundCogIcon, IdCardIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, Outlet, useLocation } from 'react-router-dom'

import { useAuth } from '@/auth/use-auth'
import { AppAvatar } from '@/components/app-avatar'
import { AppButton } from '@/components/app-button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { ensaioBasePath, ensaioPath, isEnsaioHost, mainUrl } from '@/ensaio/frame'
import { cn } from '@/lib/utils'

import { ensaioNav } from './ensaio-nav'

function useEnsaioLocation() {
  const { pathname } = useLocation()
  const inner = pathname.slice(ensaioBasePath.length) || '/'
  return inner.startsWith('/') ? inner : `/${inner}`
}

function EnsaioLogo() {
  const { t } = useTranslation('ensaio')

  return (
    <Link to={ensaioPath('/')} className="flex items-center gap-3 rounded-lg focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none">
      <span className="flex size-9 items-center justify-center rounded-lg bg-primary font-heading text-sm font-bold text-primary-foreground md:size-10 md:text-base">
        /NV
      </span>
      <span className="flex flex-col">
        <span className="font-heading text-xl leading-none font-bold tracking-[0.1em] md:text-2xl">{t('title').toUpperCase()}</span>
        <span className="hidden text-xs text-muted-foreground md:block">{t('tagline')}</span>
      </span>
    </Link>
  )
}

function PostMenu({ compact }: { compact: boolean }) {
  const { t } = useTranslation('ensaio')

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <AppButton size={compact ? 'sm' : 'md'} className="font-heading tracking-wider uppercase">
          <PlusIcon />
          {t(compact ? 'menu.postShort' : 'menu.post')}
        </AppButton>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-64">
        <DropdownMenuItem asChild>
          <Link to={ensaioPath('/minha-ficha')}>
            <IdCardIcon />
            {t('menu.myProfile')}
          </Link>
        </DropdownMenuItem>
        <DropdownMenuItem asChild>
          <Link to={ensaioPath('/vagas/nova')}>
            <MegaphoneIcon />
            {t('menu.openOpening')}
          </Link>
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

function AccountMenu() {
  const { t } = useTranslation('ensaio')
  const { user, logout } = useAuth()

  if (!user) return null

  const handleLogout = async () => {
    await logout()
    window.location.assign(mainUrl('/entrar'))
  }

  const mainLink = (path: string) => (isEnsaioHost ? { href: mainUrl(path) } : null)

  return (
    <DropdownMenu>
      <DropdownMenuTrigger className="rounded-full p-0.5 focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none" aria-label={user.name}>
        <AppAvatar name={user.name} src={user.avatarUrl} size="md" />
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-60">
        <DropdownMenuLabel className="flex flex-col">
          <span className="truncate">{user.name}</span>
          <span className="app-meta truncate font-normal">@{user.nickname}</span>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem asChild>
          {mainLink('/feed') ? (
            <a href={mainUrl('/feed')}>
              <HouseIcon />
              {t('menu.backToMain')}
            </a>
          ) : (
            <Link to="/feed">
              <HouseIcon />
              {t('menu.backToMain')}
            </Link>
          )}
        </DropdownMenuItem>
        <DropdownMenuItem asChild>
          {mainLink('/minha-conta') ? (
            <a href={mainUrl('/minha-conta')}>
              <UserRoundCogIcon />
              {t('menu.account')}
            </a>
          ) : (
            <Link to="/minha-conta">
              <UserRoundCogIcon />
              {t('menu.account')}
            </Link>
          )}
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem onSelect={() => void handleLogout()}>
          <LogOutIcon />
          {t('menu.logout')}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

export function EnsaioShell() {
  const { t } = useTranslation('ensaio')
  const current = useEnsaioLocation()

  return (
    <div className="dark ensaio flex min-h-dvh flex-col bg-background text-foreground">
      <header className="sticky top-0 z-30 border-b bg-background/95 pt-[env(safe-area-inset-top)] backdrop-blur">
        <div className="mx-auto flex h-16 max-w-7xl items-center justify-between gap-4 px-4 md:h-[72px] md:px-12">
          <EnsaioLogo />
          <nav aria-label={t('nav.label')} className="hidden h-full items-stretch gap-8 md:flex">
            {ensaioNav.map((item) => {
              const active = item.matches(current)
              return (
                <Link
                  key={item.path}
                  to={ensaioPath(item.path)}
                  aria-current={active ? 'page' : undefined}
                  className={cn(
                    'flex items-center border-b-[3px] pt-[3px] text-[15px] transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/50',
                    active ? 'border-primary font-semibold text-foreground' : 'border-transparent text-muted-foreground hover:text-foreground',
                  )}
                >
                  {t(item.labelKey)}
                </Link>
              )
            })}
          </nav>
          <div className="flex items-center gap-3">
            <span className="hidden md:block">
              <PostMenu compact={false} />
            </span>
            <span className="md:hidden">
              <PostMenu compact />
            </span>
            <AccountMenu />
          </div>
        </div>
      </header>

      <main className="ensaio-cork flex-1 pb-24 md:pb-12">
        <div className="mx-auto w-full max-w-7xl px-4 py-6 md:px-12 md:py-10">
          <Outlet />
        </div>
      </main>

      <nav
        aria-label={t('nav.label')}
        className="fixed inset-x-0 bottom-0 z-30 grid grid-cols-3 border-t bg-background pb-[env(safe-area-inset-bottom)] md:hidden"
      >
        {ensaioNav.map((item) => {
          const active = item.matches(current)
          const Icon = item.icon
          return (
            <Link
              key={item.path}
              to={ensaioPath(item.path)}
              aria-current={active ? 'page' : undefined}
              className={cn('flex h-16 flex-col items-center justify-center gap-1 text-xs', active ? 'font-semibold text-primary' : 'text-muted-foreground')}
            >
              <Icon className="size-5" />
              {t(item.labelKey)}
            </Link>
          )
        })}
      </nav>
    </div>
  )
}
