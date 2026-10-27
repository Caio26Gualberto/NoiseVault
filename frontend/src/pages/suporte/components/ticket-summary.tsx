import { useTranslation } from 'react-i18next'

import { AppBadge } from '@/components/app-badge'
import { AppMarkdown } from '@/components/app-markdown'
import { AppMediaGrid } from '@/components/app-media-grid'
import type { MediaItem } from '@/components/app-media-grid'
import { formatDateTime } from '@/lib/format'
import { MediaKind } from '@/pages/feed/media-kind'
import { MediaStatus } from '@/pages/feed/media-status'
import type { MediaDto } from '@/pages/feed/services/media-service'
import type { SupportTicketDto } from '@/pages/ajuda/services/suporte-service'
import { supportStatusTone, useSupportLabels } from '@/pages/ajuda/support-enums'

function toMediaItem(media: MediaDto): MediaItem {
  return {
    id: media.id,
    kind: media.kind === MediaKind.video ? 'video' : 'photo',
    url: media.url,
    posterUrl: media.posterUrl,
    durationSeconds: media.durationSeconds,
    state: media.status === MediaStatus.ready ? 'ready' : media.status === MediaStatus.failed ? 'failed' : 'processing',
  }
}

export function TicketStatusBadge({ ticket }: { ticket: Pick<SupportTicketDto, 'status'> }) {
  const labels = useSupportLabels()
  return <AppBadge tone={supportStatusTone[ticket.status]}>{labels.status(ticket.status)}</AppBadge>
}

export function TicketSummary({ ticket }: { ticket: SupportTicketDto }) {
  const { t } = useTranslation('suporte')
  const labels = useSupportLabels()

  return (
    <section className="flex flex-col gap-4">
      <div className="flex flex-wrap gap-1.5">
        {ticket.tags.map((tag) => (
          <AppBadge key={tag}>{labels.tag(tag)}</AppBadge>
        ))}
      </div>
      <AppMarkdown text={ticket.description} />
      {ticket.areas.length > 0 ? (
        <p className="text-sm text-muted-foreground">
          {t('detail.areas')}: {ticket.areas.map(labels.area).join(', ')}
        </p>
      ) : null}
      {ticket.media && ticket.media.length > 0 ? <AppMediaGrid items={ticket.media.map(toMediaItem)} /> : null}
      <p className="app-meta">
        {t('detail.openedAt', { date: formatDateTime(ticket.createdAt) })}
        {ticket.closedAt ? ` · ${t('detail.closedAt', { date: formatDateTime(ticket.closedAt) })}` : null}
      </p>
    </section>
  )
}
