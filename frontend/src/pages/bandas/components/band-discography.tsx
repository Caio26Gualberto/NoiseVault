import { DiscIcon, ExternalLinkIcon } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { AppBadge } from '@/components/app-badge'
import { AppSkeleton } from '@/components/app-skeleton'

import { releaseKindKey } from '../release-kind'
import { getBandDiscography } from '../services/bandas-service'
import type { BandDiscographyDto, BandReleaseDto } from '../services/bandas-service'
import { ReleaseDialog } from './release-dialog'
import { SpotifyEmbed, SpotifyLogo } from './spotify-embed'

type BandDiscographyProps = {
  slug: string
}

export function BandDiscography({ slug }: BandDiscographyProps) {
  const { t } = useTranslation('bandas')
  const [discography, setDiscography] = useState<BandDiscographyDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [selected, setSelected] = useState<BandReleaseDto | null>(null)

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
            <a
              href={featured.track.externalUrl ?? undefined}
              target="_blank"
              rel="noopener noreferrer"
              className="flex items-center gap-3 rounded-lg p-1 hover:bg-accent/50"
            >
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

      <ul className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4">
        {discography.releases.map((release) => (
          <li key={release.id}>
            <button
              type="button"
              onClick={() => setSelected(release)}
              className="group flex w-full flex-col gap-2 rounded-xl text-left focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
            >
              <span className="aspect-square w-full overflow-hidden rounded-xl border bg-muted">
                {release.coverUrl ? (
                  <img src={release.coverUrl} alt="" loading="lazy" className="size-full object-cover transition-transform group-hover:scale-[1.03]" />
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
        ))}
      </ul>

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
