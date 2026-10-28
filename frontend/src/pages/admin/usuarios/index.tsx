import { SearchIcon, UsersIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { providerNames } from '@/auth/external/providers'
import { AppAvatar } from '@/components/app-avatar'
import { AppBadge } from '@/components/app-badge'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppPageHeader } from '@/components/app-page-header'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { formatDateTime, formatRelative } from '@/lib/format'

import { useUsuariosAdminPage } from './hooks/use-usuarios-admin-page'
import type { UserSignupSummaryDto } from './services/usuarios-admin-service'

const summaryTiles: { key: keyof Omit<UserSignupSummaryDto, 'unconfirmed'>; labelKey: string }[] = [
  { key: 'today', labelKey: 'summary.today' },
  { key: 'last7Days', labelKey: 'summary.last7Days' },
  { key: 'last30Days', labelKey: 'summary.last30Days' },
  { key: 'total', labelKey: 'summary.total' },
]

export function UsuariosAdminPage() {
  const { t } = useTranslation('usuarios')
  const page = useUsuariosAdminPage()

  return (
    <div className="app-page-enter flex flex-col">
      <AppPageHeader title={t('title')} description={t('description')} />

      <div className="mb-2 grid grid-cols-2 gap-3 md:grid-cols-4">
        {summaryTiles.map((tile) => (
          <div key={tile.key} className="flex flex-col gap-1 rounded-xl border bg-card p-4">
            <span className="app-meta">{t(tile.labelKey)}</span>
            <span className="app-h2 tabular-nums">{page.summary ? page.summary[tile.key] : '–'}</span>
          </div>
        ))}
      </div>
      <p className="mb-6 min-h-5 text-sm text-muted-foreground">
        {page.summary && page.summary.unconfirmed > 0 ? t('summary.unconfirmed', { count: page.summary.unconfirmed }) : null}
      </p>

      <div className="relative mb-6">
        <SearchIcon className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
        <input
          type="search"
          value={page.search}
          onChange={(event) => page.changeSearch(event.target.value)}
          placeholder={t('searchPlaceholder')}
          aria-label={t('searchPlaceholder')}
          className="h-10 w-full rounded-lg border bg-transparent pr-3 pl-9 text-sm placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
        />
      </div>

      {page.loading ? (
        <AppSkeleton preset="list" count={5} />
      ) : page.items.length === 0 ? (
        <AppEmptyState
          icon={UsersIcon}
          title={t(page.search ? 'emptyTitle' : 'emptyAllTitle')}
          description={t(page.search ? 'emptyDescription' : 'emptyAllDescription')}
        />
      ) : (
        <ul className="flex flex-col divide-y rounded-xl border">
          {page.items.map((user) => (
            <li key={user.id} className="flex gap-3 p-3 sm:p-4">
              <AppAvatar name={user.name} src={user.avatarUrl} size="lg" />
              <div className="flex min-w-0 flex-1 flex-col gap-1">
                <div className="flex flex-wrap items-baseline gap-x-2">
                  <Link to={`/perfil/${user.nickname}`} className="truncate font-medium hover:underline">
                    {user.name}
                  </Link>
                  <span className="truncate text-sm text-muted-foreground">@{user.nickname}</span>
                </div>
                <span className="truncate text-sm text-muted-foreground">{user.email}</span>
                <span className="app-meta" title={formatDateTime(user.createdAt)}>
                  {[t('joined', { when: formatRelative(user.createdAt) }), user.cityName].filter(Boolean).join(' · ')}
                </span>
                <div className="flex flex-wrap gap-1.5 pt-1">
                  {user.hasPassword ? <AppBadge>{t('methods.password')}</AppBadge> : null}
                  {(user.providers ?? []).map((provider) => (
                    <AppBadge key={provider} tone="info">
                      {providerNames[provider]}
                    </AppBadge>
                  ))}
                  {user.emailConfirmed === false ? <AppBadge tone="warning">{t('unconfirmed')}</AppBadge> : null}
                </div>
              </div>
            </li>
          ))}
        </ul>
      )}

      <AppPagination page={page.page} totalPages={page.totalPages} totalCount={page.totalCount} onChange={page.setPage} loading={page.loading} />
    </div>
  )
}
