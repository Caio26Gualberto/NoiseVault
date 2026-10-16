import { ChevronLeftIcon, ChevronRightIcon, DiscIcon, ExternalLinkIcon } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { AppBadge } from '@/components/app-badge'
import { AppSkeleton } from '@/components/app-skeleton'
import { useIsMobile } from '@/hooks/use-media-query'
import { cn } from '@/lib/utils'

import { releaseKindKey } from '../release-kind'
import { getBandDiscography } from '../services/bandas-service'
import type { BandDiscographyDto, BandReleaseDto } from '../services/bandas-service'
import { ReleaseDialog } from './release-dialog'
import { SpotifyEmbed, SpotifyLogo } from './spotify-embed'

type BandDiscographyProps = {
  slug: string
}

const arrowClass =
  'pointer-events-auto absolute top-1/2 z-10 flex size-10 -translate-y-1/2 items-center justify-center rounded-full border bg-card/95 text-foreground shadow-sm transition-all hover:border-primary hover:text-primary focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none disabled:pointer-events-none disabled:opacity-0'

export function BandDiscography({ slug }: BandDiscographyProps) {
  const { t } = useTranslation('bandas')
  const isMobile = useIsMobile()
  const [discography, setDiscography] = useState<BandDiscographyDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [selected, setSelected] = useState<BandReleaseDto | null>(null)
  const [page, setPage] = useState(0)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    getBandDiscography(slug)
      .then((data) => {
        if (!cancelled) setDiscography(data)
      })
      .catch(() => {
        if (!cancelled) setDiscography(null)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [slug])

  if (loading) return <AppSkeleton preset="list" count={2} />
  if (!discography || discography.releases.length === 0) return null

  const featured = discography.featuredTrack
  const pageSize = isMobile ? 2 : 4
  const totalPages = Math.ceil(discography.releases.length / pageSize)
  const current = Math.min(page, totalPages - 1)
  const hasSpotify = discography.releases.some((release) => release.spotifyAlbumId)

  return (
    <section className="flex flex-col gap-3">
      <h2 className="app-h3">{t('discography.title')}</h2>

      {featured ? (
        <div className="flex flex-col gap-2 rounded-xl border bg-card p-3">
          <span className="app-meta">{t('discography.featuredTrack')}</span>
          {featured.track.spotifyTrackId ? (
            <SpotifyEmbed trackId={featured.track.spotifyTrackId} title={featured.track.title} />
          ) : (
            <a href={featured.track.externalUrl ?? undefined} target="_blank" rel="noopener noreferrer" className="flex items-center gap-3 rounded-lg p-1 hover:bg-accent/50">
              <span className="size-14 shrink-0 overflow-hidden rounded-md bg-muted">
                {featured.coverUrl ? <img src={featured.coverUrl} alt="" className="size-full object-cover" /> : null}
              </span>
              <span className="flex min-w-0 flex-1 flex-col">
                <span className="truncate font-medium">{featured.track.title}</span>
                <span className="app-meta truncate">{featured.releaseTitle}</span>
              </span>
              {featured.track.externalUrl ? <ExternalLinkIcon className="size-4 text-muted-foreground" /> : null}
            </a>
          )}
        </div>
      ) : null}

      <div className="relative px-12 md:px-14">
        <div className="pointer-events-none absolute inset-x-12 top-0 grid grid-cols-2 gap-3 md:inset-x-14 md:grid-cols-4">
          {Array.from({ length: pageSize }, (_, index) => (
            <div key={index} className="relative aspect-square">
              {index === 0 ? (
                <button type="button" aria-label={t('discography.previous')} disabled={current === 0} onClick={() => setPage(current - 1)} className={cn(arrowClass, '-left-12 md:-left-14')}>
                  <ChevronLeftIcon className="size-5" />
                </button>
              ) : null}
              {index === pageSize - 1 ? (
                <button type="button" aria-label={t('discography.next')} disabled={current >= totalPages - 1} onClick={() => setPage(current + 1)} className={cn(arrowClass, '-right-12 md:-right-14')}>
                  <ChevronRightIcon className="size-5" />
                </button>
              ) : null}
            </div>
          ))}
        </div>
        <div className="overflow-hidden">
          <ul
            className="-mx-1.5 flex transition-transform duration-1000 ease-[cubic-bezier(0.22,1,0.36,1)] motion-reduce:transition-none"
            style={{ transform: `translateX(-${current * 100}%)` }}
          >
            {discography.releases.map((release, index) => {
              const onPage = Math.floor(index / pageSize) === current
              return (
                <li key={release.id} className="w-1/2 shrink-0 px-1.5 md:w-1/4" aria-hidden={!onPage}>
                  <button
                    type="button"
                    tabIndex={onPage ? 0 : -1}
                    onClick={() => setSelected(release)}
                    className="group flex w-full flex-col gap-2 rounded-xl text-left focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
                  >
                    <span className="aspect-square w-full overflow-hidden rounded-xl border bg-muted">
                      {release.coverUrl ? (
                        <img src={release.coverUrl} alt="" loading="lazy" className="size-full object-cover transition-transform duration-300 group-hover:scale-[1.04]" />
                      ) : (
                        <span className="flex size-full items-center justify-center text-muted-foreground">
                          <DiscIcon className="size-10" />
                        </span>
                      )}
                    </span>
                    <span className="flex flex-col gap-0.5 px-0.5">
                      <span className="truncate text-sm font-medium">{release.title}</span>
                      <span className="app-meta flex items-center gap-1.5">
                        <AppBadge tone="neutral">{t(`discography.kinds.${releaseKindKey[release.kind]}`)}</AppBadge>
                        <span className="tabular-nums">{release.year}</span>
                      </span>
                    </span>
                  </button>
                </li>
              )
            })}
          </ul>
        </div>
      </div>

      {totalPages > 1 ? (
        <div className="flex items-center justify-center gap-1.5" aria-label={t('discography.pageOf', { page: current + 1, total: totalPages })}>
          {Array.from({ length: totalPages }, (_, index) => (
            <button
              key={index}
              type="button"
              aria-label={t('discography.pageOf', { page: index + 1, total: totalPages })}
              aria-current={index === current}
              onClick={() => setPage(index)}
              className={cn('h-1.5 rounded-full transition-all', index === current ? 'w-6 bg-primary' : 'w-1.5 bg-muted-foreground/40 hover:bg-muted-foreground')}
            />
          ))}
        </div>
      ) : null}

      {hasSpotify ? (
        <p className="app-meta flex items-center gap-1.5">
          <SpotifyLogo className="size-4" />
          {discography.spotifyArtistUrl ? (
            <a href={discography.spotifyArtistUrl} target="_blank" rel="noopener noreferrer" className="hover:underline">
              {t('discography.attributionLink')}
            </a>
          ) : (
            t('discography.attribution')
          )}
        </p>
      ) : null}

      <ReleaseDialog
        release={selected}
        onOpenChange={(open) => {
          if (!open) setSelected(null)
        }}
      />
    </section>
  )
}
