import { NewspaperIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { AppCoverImage } from '@/components/app-cover-image'
import { formatTime } from '@/lib/format'

import type { NewsArticleDto } from '../services/noticias-service'

type NewsCardProps = {
  article: NewsArticleDto
}

export function NewsCard({ article }: NewsCardProps) {
  const { t } = useTranslation('noticias')

  return (
    <article className="group flex flex-col overflow-hidden rounded-xl border bg-card">
      <a
        href={article.url}
        target="_blank"
        rel="noopener noreferrer"
        aria-label={t('openArticle', { title: article.title })}
        className="flex flex-1 flex-col focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
      >
        <div className="relative aspect-video overflow-hidden bg-muted">
          {article.imageUrl ? (
            <AppCoverImage src={article.imageUrl} poster={false} loading="lazy" className="transition-transform duration-300 group-hover:scale-[1.03]" />
          ) : (
            <span className="flex size-full items-center justify-center text-muted-foreground">
              <NewspaperIcon className="size-8" />
            </span>
          )}
        </div>
        <div className="flex flex-1 flex-col gap-2 p-4">
          <h3 className="app-h3 line-clamp-3 group-hover:text-primary">{article.title}</h3>
          <p className="app-meta mt-auto truncate">
            {article.author ? t('byline', { author: article.author, time: formatTime(article.publishedAt) }) : formatTime(article.publishedAt)}
          </p>
        </div>
      </a>
    </article>
  )
}
