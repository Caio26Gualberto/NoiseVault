import { ChevronLeftIcon, ChevronRightIcon, Loader2Icon } from 'lucide-react'
import { useEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'

import { AppButton } from '@/components/app-button'

type AppPaginationProps = {
  page: number
  totalPages: number
  totalCount: number
  onChange: (page: number) => void
  mode?: 'pages' | 'loadMore' | 'infinite'
  loading?: boolean
}

export function AppPagination({ page, totalPages, totalCount, onChange, mode = 'pages', loading = false }: AppPaginationProps) {
  const { t } = useTranslation()

  if (totalPages <= 1) return null

  if (mode === 'infinite') {
    return <InfiniteSentinel hasMore={page < totalPages} loading={loading} onMore={() => onChange(page + 1)} label={t('loading')} />
  }

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

type InfiniteSentinelProps = {
  hasMore: boolean
  loading: boolean
  onMore: () => void
  label: string
}

const sentinelMargin = '600px 0px'

function InfiniteSentinel({ hasMore, loading, onMore, label }: InfiniteSentinelProps) {
  const ref = useRef<HTMLDivElement>(null)
  const onMoreRef = useRef(onMore)
  useEffect(() => {
    onMoreRef.current = onMore
  }, [onMore])

  useEffect(() => {
    const element = ref.current
    if (!element || !hasMore || loading || typeof IntersectionObserver === 'undefined') return
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((entry) => entry.isIntersecting)) onMoreRef.current()
      },
      { rootMargin: sentinelMargin },
    )
    observer.observe(element)
    return () => observer.disconnect()
  }, [hasMore, loading])

  if (!hasMore) return null

  return (
    <div ref={ref} className="flex justify-center py-6" role="status" aria-label={label}>
      {loading ? <Loader2Icon className="size-6 animate-spin text-muted-foreground" /> : null}
    </div>
  )
}
