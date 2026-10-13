import { ChevronsUpDownIcon, LogOutIcon, MoonIcon, SunIcon, SunMoonIcon, UserRoundCogIcon, UserRoundIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router-dom'

import { AppAvatar } from '@/components/app-avatar'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { cn } from '@/lib/utils'

import { accountPath, loginPath } from '../auth/routes'
import { useAuth } from '../auth/use-auth'
import { useColorScheme } from '../theme/color-scheme'
import type { ColorScheme } from '../theme/color-scheme'

type UserMenuProps = {
  compact?: boolean
}

export function UserMenu({ compact = false }: UserMenuProps) {
  const { t } = useTranslation()
  const { user, logout } = useAuth()
  const { scheme, change } = useColorScheme()
  const navigate = useNavigate()

  if (!user) return null

  const handleLogout = async () => {
    await logout()
    navigate(loginPath, { replace: true })
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        className={cn(
          'flex w-full items-center gap-3 rounded-lg text-left transition-colors hover:bg-sidebar-accent focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none',
          compact ? 'w-auto p-0.5' : 'px-2 py-2',
        )}
        aria-label={user.name}
      >
        <AppAvatar name={user.name} src={user.avatarUrl} size="md" />
        {compact ? null : (
          <>
            <span className="flex min-w-0 flex-1 flex-col">
              <span className="truncate text-sm font-medium">{user.name}</span>
              <span className="app-meta truncate">@{user.nickname}</span>
            </span>
            <ChevronsUpDownIcon className="size-4 text-muted-foreground" />
          </>
        )}
      </DropdownMenuTrigger>
      <DropdownMenuContent align={compact ? 'end' : 'start'} side={compact ? 'bottom' : 'top'} className="w-56">
        <DropdownMenuLabel className="flex flex-col">
          <span className="truncate font-medium">{user.name}</span>
          <span className="app-meta truncate">{user.email}</span>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem asChild>
          <Link to={`/perfil/${user.nickname}`}>
            <UserRoundIcon />
            {t('layout:userMenu.myProfile')}
          </Link>
        </DropdownMenuItem>
        <DropdownMenuItem asChild>
          <Link to={accountPath}>
            <UserRoundCogIcon />
            {t('layout:userMenu.account')}
          </Link>
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuLabel className="app-meta font-normal">{t('layout:userMenu.theme')}</DropdownMenuLabel>
        <DropdownMenuRadioGroup value={scheme} onValueChange={(value) => change(value as ColorScheme)}>
          <DropdownMenuRadioItem value="dark">
            <MoonIcon />
            {t('layout:userMenu.themeDark')}
          </DropdownMenuRadioItem>
          <DropdownMenuRadioItem value="light">
            <SunIcon />
            {t('layout:userMenu.themeLight')}
          </DropdownMenuRadioItem>
          <DropdownMenuRadioItem value="auto">
            <SunMoonIcon />
            {t('layout:userMenu.themeAuto')}
          </DropdownMenuRadioItem>
        </DropdownMenuRadioGroup>
        <DropdownMenuSeparator />
        <DropdownMenuItem variant="destructive" onSelect={() => void handleLogout()}>
          <LogOutIcon />
          {t('layout:userMenu.logout')}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
