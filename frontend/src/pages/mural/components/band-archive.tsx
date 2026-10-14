import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { AppPagination } from '@/components/app-pagination'

import { getBandArchive } from '../services/mural-service'
import type { ArchiveEntryDto } from '../services/mural-service'
import { ArchiveCard } from './archive-card'

const pageSize = 4

export function BandArchive({ slug }: { slug: string }) {
  const { t } = useTranslation('mural')
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<ArchiveEntryDto>>(() => emptyPage(pageSize))

  useEffect(() => {
    let cancelled = false
    getBandArchive(slug, { page, pageSize, year: null, decade: null, search: '', city: null, kind: null, sort: 'oldest' })
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [slug, page])

  if (result.totalCount === 0) return null

  return (
    <section className="flex flex-col gap-3">
      <h2 className="app-h3">{t('band.title')}</h2>
      <div className="grid gap-4 md:grid-cols-2">
        {result.items.map((entry) => (
          <ArchiveCard key={entry.id} entry={entry} />
        ))}
      </div>
      <AppPagination page={page} totalPages={result.totalPages} totalCount={result.totalCount} onChange={setPage} />
    </section>
  )
}
