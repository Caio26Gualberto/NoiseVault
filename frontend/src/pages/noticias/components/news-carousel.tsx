import { ChevronLeftIcon, ChevronRightIcon, NewspaperIcon } from 'lucide-react'
import { useCallback, useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { AppIconButton } from '@/components/app-button'
import { AppCoverImage } from '@/components/app-cover-image'
import { formatTime } from '@/lib/format'

import type { NewsArticleDto } from '../services/noticias-service'

type NewsCarouselProps = {
  articles: NewsArticleDto[]
}

export function NewsCarousel({ articles }: NewsCarouselProps) {
  const { t } = useTranslation('noticias')
  const scrollerRef = useRef<HTMLDivElement>(null)
  const [canPrevious, setCanPrevious] = useState(false)
  const [canNext, setCanNext] = useState(false)

  const updateEdges = useCallback(() => {
    const scroller = scrollerRef.current
    if (!scroller) return
    setCanPrevious(scroller.scrollLeft > 4)
    setCanNext(scroller.scrollLeft + scroller.clientWidth < scroller.scrollWidth - 4)
  }, [])

  useEffect(() => {
    updateEdges()
    window.addEventListener('resize', updateEdges)
    return () => window.removeEventListener('resize', updateEdges)
  }, [articles, updateEdges])

  const scrollBy = (direction: 1 | -1) => {
    const scroller = scrollerRef.current
    if (!scroller) return
    scroller.scrollBy({ left: direction * scroller.clientWidth * 0.85, behavior: 'smooth' })
  }

  return (
    <section aria-label={t('today')} className="mb-8 flex flex-col gap-3">
      <div className="flex items-center justify-between gap-3">
        <h2 className="app-h2">{t('today')}</h2>
        <div className="hidden gap-1 md:flex">
          <AppIconButton label={t('previous')} intent="neutral" size="sm" disabled={!canPrevious} onClick={() => scrollBy(-1)}>
            <ChevronLeftIcon />
          </AppIconButton>
          <AppIconButton label={t('next')} intent="neutral" size="sm" disabled={!canNext} onClick={() => scrollBy(1)}>
            <ChevronRightIcon />
          </AppIconButton>
        </div>
      </div>

      <div
        ref={scrollerRef}
        onScroll={updateEdges}
        className="-mx-4 flex snap-x snap-mandatory scroll-px-4 gap-3 overflow-x-auto px-4 pb-2 [scrollbar-width:thin] md:mx-0 md:scroll-px-0 md:px-0"
      >
        {articles.map((article) => (
          <a
            key={article.id}
            href={article.url}
            target="_blank"
            rel="noopener noreferrer"
            aria-label={t('openArticle', { title: article.title })}
            className="group relative aspect-[4/3] w-[85%] shrink-0 snap-start overflow-hidden rounded-xl border bg-muted focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none sm:aspect-video sm:w-[60%] lg:w-[46%]"
          >
            {article.imageUrl ? (
              <AppCoverImage src={article.imageUrl} poster={false} loading="lazy" className="transition-transform duration-300 group-hover:scale-[1.03]" />
            ) : (
              <span className="flex size-full items-center justify-center text-muted-foreground">
                <NewspaperIcon className="size-10" />
              </span>
            )}
            <span className="absolute inset-0 bg-gradient-to-t from-scrim/90 via-scrim/40 to-transparent" />
            <span className="absolute inset-x-0 bottom-0 flex flex-col gap-1 p-4 text-on-scrim">
              <span className="font-heading text-lg leading-tight font-semibold line-clamp-3 sm:text-xl">{article.title}</span>
              <span className="truncate text-xs text-on-scrim/75">
                {article.author ? t('byline', { author: article.author, time: formatTime(article.publishedAt) }) : formatTime(article.publishedAt)}
              </span>
            </span>
          </a>
        ))}
      </div>
    </section>
  )
}
