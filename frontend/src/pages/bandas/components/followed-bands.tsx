import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { AppAvatar } from '@/components/app-avatar'
import { AppGenreBadges } from '@/components/app-genre-badges'
import { AppPagination, defaultPageSize } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'

import { getFollowedBands } from '../services/bandas-service'
import type { BandSummaryDto } from '../services/bandas-service'

export function FollowedBands() {
  const { t } = useTranslation('bandas')
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<BandSummaryDto>>(() => emptyPage(defaultPageSize))
  const [items, setItems] = useState<BandSummaryDto[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    getFollowedBands({ page, pageSize: defaultPageSize })
      .then((data) => {
        if (cancelled) return
        setResult(data)
        setItems((previous) => {
          if (page === 1) return data.items
          const known = new Set(previous.map((band) => band.id))
          return [...previous, ...data.items.filter((band) => !known.has(band.id))]
        })
      })
      .catch(() => {
        if (!cancelled && page === 1) setItems([])
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page])

  return (
    <section className="flex flex-col gap-3">
      <h2 className="app-h3">{t('following.title')}</h2>
      {loading && items.length === 0 ? (
        <AppSkeleton preset="lines" count={2} />
      ) : items.length === 0 ? (
        <p className="app-meta">{t('following.empty')}</p>
      ) : (
        <>
          <ul className="grid gap-3 sm:grid-cols-2">
            {items.map((band) => (
              <li key={band.id}>
                <Link
                  to={`/bandas/${band.slug}`}
                  className="flex items-center gap-3 rounded-xl border bg-card p-3 transition-colors hover:bg-accent focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
                >
                  <AppAvatar name={band.name} src={band.avatarUrl} size="lg" verified={band.isVerified} />
                  <span className="flex min-w-0 flex-1 flex-col gap-1">
                    <span className="truncate font-medium">{band.name}</span>
                    <AppGenreBadges genres={band.genres} max={2} />
                  </span>
                </Link>
              </li>
            ))}
          </ul>
          <AppPagination page={page} totalPages={result.totalPages} totalCount={result.totalCount} onChange={setPage} mode="loadMore" loading={loading} />
        </>
      )}
    </section>
  )
}
