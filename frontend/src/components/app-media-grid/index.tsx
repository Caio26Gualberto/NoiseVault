import { PlayIcon } from 'lucide-react'
import { useState } from 'react'

import { AppDialog } from '@/components/app-dialog'
import { cn } from '@/lib/utils'

export type MediaItem = {
  id: string | number
  kind: 'photo' | 'video'
  url: string
  alt?: string
}

type AppMediaGridProps = {
  items: MediaItem[]
  className?: string
}

export function AppMediaGrid({ items, className }: AppMediaGridProps) {
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
        {shown.map((item, index) => (
          <button
            key={item.id}
            type="button"
            onClick={() => setOpen(item)}
            className={cn(
              'group relative block overflow-hidden bg-muted focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none',
              shown.length === 1 ? 'aspect-video' : 'aspect-[4/3]',
            )}
          >
            <img src={item.url} alt={item.alt ?? ''} loading="lazy" className="size-full object-cover transition-transform duration-300 group-hover:scale-[1.02]" />
            {item.kind === 'video' ? (
              <span className="absolute inset-0 flex items-center justify-center bg-scrim/30">
                <span className="flex size-12 items-center justify-center rounded-full bg-background/90 text-foreground">
                  <PlayIcon className="ml-0.5 size-5 fill-current" />
                </span>
              </span>
            ) : null}
            {rest > 0 && index === shown.length - 1 ? (
              <span className="absolute inset-0 flex items-center justify-center bg-scrim/60 font-heading text-2xl font-semibold text-on-scrim">
                +{rest}
              </span>
            ) : null}
          </button>
        ))}
      </div>

      <AppDialog open={open !== null} onOpenChange={() => setOpen(null)} title={open?.alt ?? ''} size="lg">
        {open ? <img src={open.url} alt={open.alt ?? ''} className="max-h-[70vh] w-full rounded-lg object-contain" /> : null}
      </AppDialog>
    </>
  )
}
