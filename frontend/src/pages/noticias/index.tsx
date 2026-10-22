import { NewspaperIcon } from 'lucide-react'
import { useMemo } from 'react'
import { Trans, useTranslation } from 'react-i18next'

import { AppEmptyState } from '@/components/app-empty-state'
import { AppPageHeader } from '@/components/app-page-header'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { useDocumentTitle } from '@/hooks/use-document-title'
import { formatWeekday } from '@/lib/format'

import { NewsCard } from './components/news-card'
import { NewsCarousel } from './components/news-carousel'
import { useNoticiasPage } from './hooks/use-noticias-page'
import { sourceName, sourceUrl } from './services/noticias-service'
import type { NewsArticleDto } from './services/noticias-service'

type DayGroup = {
  key: string
  label: string
  items: NewsArticleDto[]
}

function groupByDay(items: NewsArticleDto[]): DayGroup[] {
  const groups: DayGroup[] = []
  for (const item of items) {
    const key = new Date(item.publishedAt).toDateString()
    const last = groups[groups.length - 1]
    if (last && last.key === key) last.items.push(item)
    else groups.push({ key, label: formatWeekday(item.publishedAt), items: [item] })
  }
  return groups
}

export function NoticiasPage() {
  const { t } = useTranslation('noticias')
  const page = useNoticiasPage()
  const groups = useMemo(() => groupByDay(page.earlier), [page.earlier])
  useDocumentTitle(t('title'))

  const source = (
    <Trans
      t={t}
      i18nKey="source"
      values={{ name: sourceName }}
      components={{ link: <a href={sourceUrl} target="_blank" rel="noopener noreferrer" className="font-medium text-foreground underline-offset-4 hover:underline" /> }}
    />
  )

  return (
    <div className="app-page-enter flex flex-col">
      <AppPageHeader title={t('title')} description={t('description')} />

      {page.loading ? (
        <AppSkeleton preset="card" count={3} className="grid gap-4 md:grid-cols-2 xl:grid-cols-3" />
      ) : page.today.length === 0 && page.earlier.length === 0 ? (
        <AppEmptyState icon={NewspaperIcon} title={t('empty.title')} description={t('empty.description')} />
      ) : (
        <>
          {page.today.length > 0 ? <NewsCarousel articles={page.today} /> : null}

          {groups.map((group) => (
            <section key={group.key} aria-label={group.label} className="mb-8 flex flex-col gap-3">
              <h2 className="app-h3 text-muted-foreground first-letter:uppercase">{group.label}</h2>
              <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                {group.items.map((article) => (
                  <NewsCard key={article.id} article={article} />
                ))}
              </div>
            </section>
          ))}

          <AppPagination
            mode="infinite"
            page={page.page}
            totalPages={page.totalPages}
            totalCount={page.totalCount}
            onChange={page.setPage}
            loading={page.earlierLoading}
          />
        </>
      )}

      <p className="app-meta mt-4 border-t pt-4">{source}</p>
    </div>
  )
}
