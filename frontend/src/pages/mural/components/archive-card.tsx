import { CameraIcon, CassetteTapeIcon, HeartIcon, MapPinIcon, UsersRoundIcon, VideoIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { cn } from '@/lib/utils'
import { formatCompact } from '@/lib/format'
import { MediaKind } from '@/pages/feed/media-kind'
import { formatCity } from '@/pages/perfil/city'

import { archiveDateBadge } from '../archive-date'
import type { ArchiveEntryDto } from '../services/mural-service'

export function ArchiveCard({ entry }: { entry: ArchiveEntryDto }) {
  const { t } = useTranslation('mural')
  const badge = archiveDateBadge(entry)
  const coverUrl = entry.cover ? (entry.cover.kind === MediaKind.video ? entry.cover.posterUrl : entry.cover.url) : null
  const where = [entry.venue, formatCity(entry)].filter(Boolean).join(' · ')

  return (
    <article className="group flex flex-col overflow-hidden rounded-xl border bg-card transition-colors hover:border-primary/50">
      <Link to={`/mural/${entry.id}`} className="relative block aspect-[4/3] overflow-hidden bg-muted focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none">
        {coverUrl ? (
          <img src={coverUrl} alt="" loading="lazy" className="size-full object-cover sepia-[.25] transition-all duration-500 group-hover:scale-[1.03] group-hover:sepia-0" />
        ) : (
          <span className="flex size-full items-center justify-center text-muted-foreground">
            <CassetteTapeIcon className="size-12" />
          </span>
        )}
        <span className="absolute top-3 left-3 flex min-w-14 flex-col items-center rounded-lg bg-background/95 px-2.5 py-1.5 text-center leading-none">
          <span className="font-heading text-lg font-bold uppercase tabular-nums">{badge.top}</span>
          {badge.bottom ? <span className="text-[11px] font-semibold tracking-wider text-muted-foreground tabular-nums">{badge.bottom}</span> : null}
        </span>
        <span className="absolute right-3 bottom-3 flex gap-1.5">
          {entry.photoCount > 0 ? (
            <span className="inline-flex items-center gap-1 rounded-full bg-scrim/70 px-2 py-0.5 text-xs text-on-scrim tabular-nums">
              <CameraIcon className="size-3.5" />
              {entry.photoCount}
            </span>
          ) : null}
          {entry.videoCount > 0 ? (
            <span className="inline-flex items-center gap-1 rounded-full bg-scrim/70 px-2 py-0.5 text-xs text-on-scrim tabular-nums">
              <VideoIcon className="size-3.5" />
              {entry.videoCount}
            </span>
          ) : null}
        </span>
      </Link>

      <div className="flex flex-1 flex-col gap-2 p-4">
        <Link to={`/mural/${entry.id}`} className="app-h3 line-clamp-2 hover:text-primary">
          {entry.title}
        </Link>
        {where ? (
          <p className="app-meta inline-flex items-center gap-1.5">
            <MapPinIcon className="size-3.5 shrink-0" />
            <span className="truncate">{where}</span>
          </p>
        ) : null}
        {entry.bands.length > 0 ? <p className="app-meta line-clamp-1">{entry.bands.map((band) => band.name).join(' · ')}</p> : null}
        <div className="mt-auto flex items-center gap-4 border-t pt-3 text-sm text-muted-foreground tabular-nums">
          <span className={cn('inline-flex items-center gap-1.5', entry.attendedByMe && 'text-primary')} title={t('attendance.count', { count: entry.attendanceCount })}>
            <UsersRoundIcon className="size-4" />
            {formatCompact(entry.attendanceCount)}
          </span>
          <span className={cn('inline-flex items-center gap-1.5', entry.likedByMe && 'text-primary')}>
            <HeartIcon className={cn('size-4', entry.likedByMe && 'fill-current')} />
            {formatCompact(entry.likeCount)}
          </span>
        </div>
      </div>
    </article>
  )
}
