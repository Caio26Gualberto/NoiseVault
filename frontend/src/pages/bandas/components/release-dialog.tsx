import { DiscIcon, ExternalLinkIcon, PlayIcon } from 'lucide-react'
import { useState } from 'react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import { AppBadge } from '@/components/app-badge'
import { AppDialog } from '@/components/app-dialog'
import { formatDuration } from '@/lib/format'

import { ReleaseSource, releaseKindKey } from '../release-kind'
import type { BandReleaseDto } from '../services/bandas-service'
import { SpotifyEmbed, SpotifyLogo } from './spotify-embed'

type ReleaseDialogProps = {
  release: BandReleaseDto | null
  onOpenChange: (open: boolean) => void
}

export function ReleaseDialog({ release, onOpenChange }: ReleaseDialogProps) {
  const { t } = useTranslation('bandas')
  const [playingId, setPlayingId] = useState<number | null>(null)

  if (!release) return null

  const spotify = release.source === ReleaseSource.spotify
  const playing = release.tracks.find((track) => track.id === playingId) ?? null

  return (
    <AppDialog
      open
      onOpenChange={(open) => {
        if (!open) setPlayingId(null)
        onOpenChange(open)
      }}
      title={release.title}
      description={`${t(`discography.kinds.${releaseKindKey[release.kind]}`)} · ${release.year}`}
      size="lg"
    >
      <div className="flex flex-col gap-4">
        <div className="flex gap-4">
          <span className="size-24 shrink-0 overflow-hidden rounded-lg bg-muted sm:size-32">
            {release.coverUrl ? (
              <img src={release.coverUrl} alt="" className="size-full object-cover" />
            ) : (
              <span className="flex size-full items-center justify-center text-muted-foreground">
                <DiscIcon className="size-8" />
              </span>
            )}
          </span>
          <div className="flex min-w-0 flex-1 flex-col gap-2 text-sm">
            <span className="app-meta">{t('discography.trackCount', { count: release.tracks.length })}</span>
            {release.externalUrl ? (
              <a href={release.externalUrl} target="_blank" rel="noopener noreferrer" className="inline-flex items-center gap-1.5 text-primary hover:underline">
                {spotify ? <SpotifyLogo className="size-4" /> : <ExternalLinkIcon className="size-4" />}
                {spotify ? t('discography.openSpotify') : t('discography.openLink')}
              </a>
            ) : null}
            {playing?.spotifyTrackId ? <SpotifyEmbed trackId={playing.spotifyTrackId} title={playing.title} /> : null}
          </div>
        </div>

        <ol className="flex max-h-[50vh] flex-col divide-y overflow-y-auto rounded-xl border">
          {release.tracks.map((track) => {
            const active = track.id === playingId
            const playable = spotify && track.spotifyTrackId
            return (
              <li key={track.id}>
                {playable ? (
                  <button
                    type="button"
                    onClick={() => setPlayingId(active ? null : track.id)}
                    aria-pressed={active}
                    className={`flex w-full items-center gap-3 px-3 py-2 text-left text-sm hover:bg-accent/50 focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none ${active ? 'bg-accent/60' : ''}`}
                  >
                    <TrackRow track={track} icon={<PlayIcon className="size-3.5" />} featuredLabel={t('discography.featured')} />
                  </button>
                ) : track.externalUrl ? (
                  <a
                    href={track.externalUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="flex w-full items-center gap-3 px-3 py-2 text-sm hover:bg-accent/50 focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
                  >
                    <TrackRow track={track} icon={<ExternalLinkIcon className="size-3.5" />} featuredLabel={t('discography.featured')} />
                  </a>
                ) : (
                  <div className="flex w-full items-center gap-3 px-3 py-2 text-sm">
                    <TrackRow track={track} icon={null} featuredLabel={t('discography.featured')} />
                  </div>
                )}
              </li>
            )
          })}
        </ol>

        {spotify ? (
          <p className="app-meta flex items-center gap-1.5">
            <SpotifyLogo className="size-4" />
            {t('discography.attribution')}
          </p>
        ) : null}
      </div>
    </AppDialog>
  )
}

function TrackRow({ track, icon, featuredLabel }: { track: BandReleaseDto['tracks'][number]; icon: ReactNode; featuredLabel: string }) {
  return (
    <>
      <span className="w-6 shrink-0 text-right text-muted-foreground tabular-nums">{track.number}</span>
      <span className="flex min-w-0 flex-1 items-center gap-2">
        <span className="truncate">{track.title}</span>
        {track.isFeatured ? <AppBadge tone="highlight">{featuredLabel}</AppBadge> : null}
      </span>
      {track.durationSeconds ? <span className="shrink-0 text-muted-foreground tabular-nums">{formatDuration(track.durationSeconds)}</span> : null}
      {icon ? <span className="shrink-0 text-muted-foreground">{icon}</span> : null}
    </>
  )
}
