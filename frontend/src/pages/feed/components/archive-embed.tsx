import { CameraIcon, CassetteTapeIcon, VideoIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { formatArchiveDate } from '@/pages/mural/archive-date'
import type { ArchiveSummaryDto } from '@/pages/mural/services/mural-service'

import { MediaKind } from '../media-kind'

export function ArchiveEmbed({ entry }: { entry: ArchiveSummaryDto }) {
  const { t } = useTranslation('feed')
  const coverUrl = entry.cover ? (entry.cover.kind === MediaKind.video ? entry.cover.posterUrl : entry.cover.url) : null

  return (
    <Link
      to={`/mural/${entry.id}`}
      className="group flex flex-col overflow-hidden rounded-lg border transition-colors hover:border-primary/60 focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
    >
      <div className="relative aspect-[4/3] overflow-hidden bg-muted sm:aspect-video">
        {coverUrl ? (
          <img src={coverUrl} alt="" loading="lazy" className="size-full object-cover sepia-[.25] transition-all duration-500 group-hover:sepia-0" />
        ) : (
          <span className="flex size-full items-center justify-center text-muted-foreground">
            <CassetteTapeIcon className="size-10" />
          </span>
        )}
        <span className="absolute top-3 left-3 inline-flex items-center gap-1.5 rounded-full bg-primary px-2.5 py-1 font-heading text-xs tracking-widest text-primary-foreground uppercase">
          <CassetteTapeIcon className="size-3.5" />
          {t('archive.badge')}
        </span>
      </div>
      <div className="flex flex-col gap-1 p-3">
        <span className="font-heading text-sm tracking-widest text-primary uppercase">{formatArchiveDate(entry)}</span>
        <span className="app-h3 line-clamp-2 group-hover:text-primary">{entry.title}</span>
        <span className="app-meta inline-flex items-center gap-3">
          {entry.photoCount > 0 ? (
            <span className="inline-flex items-center gap-1">
              <CameraIcon className="size-3.5" />
              {t('archive.photos', { count: entry.photoCount })}
            </span>
          ) : null}
          {entry.videoCount > 0 ? (
            <span className="inline-flex items-center gap-1">
              <VideoIcon className="size-3.5" />
              {t('archive.videos', { count: entry.videoCount })}
            </span>
          ) : null}
        </span>
      </div>
    </Link>
  )
}
