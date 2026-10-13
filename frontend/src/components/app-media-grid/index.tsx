import { CircleAlertIcon, LoaderCircleIcon, PlayIcon } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { AppDialog } from '@/components/app-dialog'
import { formatDuration } from '@/lib/format'
import { cn } from '@/lib/utils'

export type MediaItem = {
  id: string | number
  kind: 'photo' | 'video'
  url: string | null
  posterUrl?: string | null
  durationSeconds?: number | null
  state?: 'ready' | 'processing' | 'failed'
  alt?: string
}

type AppMediaGridProps = {
  items: MediaItem[]
  className?: string
}

export function AppMediaGrid({ items, className }: AppMediaGridProps) {
  const { t } = useTranslation('common')
  const [open, setOpen] = useState<MediaItem | null>(null)

  if (items.length === 0) return null

  const shown = items.slice(0, 4)
  const rest = items.length - shown.length
  const layout =
    shown.length === 1
      ? 'grid-cols-1'
      : shown.length === 2
        ? 'grid-cols-2'
        : 'grid-cols-2 [&>*:first-child]:col-span-2'

  return (
    <>
      <div className={cn('grid gap-1 overflow-hidden rounded-lg', layout, className)}>
        {shown.map((item, index) => {
          const state = item.state ?? 'ready'
          const preview = item.kind === 'video' ? item.posterUrl : item.url
          const frame = cn(
            'group relative block overflow-hidden bg-muted',
            shown.length === 1 ? 'aspect-video' : 'aspect-[4/3]',
          )

          if (state !== 'ready' || !item.url) {
            return (
              <div key={item.id} className={cn(frame, 'flex flex-col items-center justify-center gap-2 text-sm text-muted-foreground')}>
                {state === 'failed' ? <CircleAlertIcon className="size-6 text-destructive" /> : <LoaderCircleIcon className="size-6 animate-spin" />}
                <span>{state === 'failed' ? t('media.failed') : t('media.processing')}</span>
              </div>
            )
          }

          return (
            <button
              key={item.id}
              type="button"
              onClick={() => setOpen(item)}
              aria-label={item.kind === 'video' ? t('media.play') : t('media.open')}
              className={cn(frame, 'focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none')}
            >
              {preview ? (
                <img src={preview} alt={item.alt ?? ''} loading="lazy" className="size-full object-cover transition-transform duration-300 group-hover:scale-[1.02]" />
              ) : null}
              {item.kind === 'video' ? (
                <span className="absolute inset-0 flex items-center justify-center bg-scrim/30">
                  <span className="flex size-12 items-center justify-center rounded-full bg-background/90 text-foreground">
                    <PlayIcon className="ml-0.5 size-5 fill-current" />
                  </span>
                  {item.durationSeconds ? (
                    <span className="absolute right-2 bottom-2 rounded bg-scrim/70 px-1.5 py-0.5 text-xs text-on-scrim tabular-nums">
                      {formatDuration(item.durationSeconds)}
                    </span>
                  ) : null}
                </span>
              ) : null}
              {rest > 0 && index === shown.length - 1 ? (
                <span className="absolute inset-0 flex items-center justify-center bg-scrim/60 font-heading text-2xl font-semibold text-on-scrim">
                  +{rest}
                </span>
              ) : null}
            </button>
          )
        })}
      </div>

      <AppDialog open={open !== null} onOpenChange={() => setOpen(null)} title={open?.alt ?? ''} size="lg">
        {open?.url ? (
          open.kind === 'video' ? (
            <video
              src={open.url}
              poster={open.posterUrl ?? undefined}
              controls
              autoPlay
              playsInline
              preload="metadata"
              className="max-h-[70vh] w-full rounded-lg bg-scrim"
            />
          ) : (
            <img src={open.url} alt={open.alt ?? ''} className="max-h-[70vh] w-full rounded-lg object-contain" />
          )
        ) : null}
      </AppDialog>
    </>
  )
}
