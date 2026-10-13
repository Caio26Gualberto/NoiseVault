import { ChevronLeftIcon, ChevronRightIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { AppButton } from '@/components/app-button'

type AppPaginationProps = {
  page: number
  totalPages: number
  totalCount: number
  onChange: (page: number) => void
  mode?: 'pages' | 'loadMore'
  loading?: boolean
}

export function AppPagination({ page, totalPages, totalCount, onChange, mode = 'pages', loading = false }: AppPaginationProps) {
  const { t } = useTranslation()

  if (totalPages <= 1) return null

  if (mode === 'loadMore') {
    if (page >= totalPages) return null
    return (
      <div className="flex justify-center py-4">
        <AppButton intent="neutral" loading={loading} onClick={() => onChange(page + 1)}>
          {t('actions.loadMore')}
        </AppButton>
      </div>
    )
  }

  return (
    <nav className="flex items-center justify-between gap-3 py-4" aria-label={t('pagination.summary', { page, totalPages })}>
      <span className="app-meta">
        {t('pagination.summary', { page, totalPages })} · {t('pagination.total', { count: totalCount })}
      </span>
      <div className="flex gap-2">
        <AppButton intent="neutral" size="sm" disabled={page <= 1 || loading} onClick={() => onChange(page - 1)}>
          <ChevronLeftIcon />
          <span className="hidden sm:inline">{t('actions.previous')}</span>
        </AppButton>
        <AppButton intent="neutral" size="sm" disabled={page >= totalPages || loading} onClick={() => onChange(page + 1)}>
          <span className="hidden sm:inline">{t('actions.next')}</span>
          <ChevronRightIcon />
        </AppButton>
      </div>
    </nav>
  )
}
