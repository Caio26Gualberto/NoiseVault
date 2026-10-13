import { BellIcon, SearchIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'

import { UserMenu } from './user-menu'
import { Wordmark } from './wordmark'

export function TopBar() {
  const { t } = useTranslation()

  return (
    <header className="sticky top-0 z-30 flex h-14 items-center gap-3 border-b bg-background/95 px-4 backdrop-blur md:px-6">
      <div className="md:hidden">
        <Wordmark size="sm" />
      </div>

      <Tooltip>
        <TooltipTrigger asChild>
          <div className="relative ml-auto hidden w-full max-w-sm md:block">
            <SearchIcon className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
            <input
              type="search"
              readOnly
              placeholder={t('layout:search.placeholder')}
              aria-label={t('layout:search.placeholder')}
              className="h-9 w-full rounded-lg border bg-muted/40 pl-9 pr-3 text-sm text-foreground placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
            />
          </div>
        </TooltipTrigger>
        <TooltipContent>{t('layout:search.soon')}</TooltipContent>
      </Tooltip>

      <div className="ml-auto flex items-center gap-1 md:ml-0">
        <Tooltip>
          <TooltipTrigger asChild>
            <Link
              to="/notificacoes"
              aria-label={t('layout:bell.label')}
              className="relative flex size-9 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:bg-muted hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
            >
              <BellIcon className="size-5" />
            </Link>
          </TooltipTrigger>
          <TooltipContent>{t('layout:bell.label')}</TooltipContent>
        </Tooltip>
        <div className="md:hidden">
          <UserMenu compact />
        </div>
      </div>
    </header>
  )
}
