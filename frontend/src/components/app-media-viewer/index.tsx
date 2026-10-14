import { ChevronLeftIcon, ChevronRightIcon, XIcon } from 'lucide-react'
import { Dialog as DialogPrimitive } from 'radix-ui'
import type { KeyboardEvent, ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import { AppVideoPlayer } from '@/components/app-video-player'
import { cn } from '@/lib/utils'

export type ViewerItem = {
  id: string | number
  kind: 'photo' | 'video'
  url: string
  posterUrl?: string | null
  alt?: string
  caption?: string | null
}

type AppMediaViewerProps = {
  items: ViewerItem[]
  index: number | null
  onIndexChange: (index: number | null) => void
}

function ViewerButton({ label, onClick, className, children }: { label: string; onClick: () => void; className?: string; children: ReactNode }) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={label}
      title={label}
      className={cn(
        'flex size-11 items-center justify-center rounded-full bg-scrim/70 text-on-scrim backdrop-blur-sm transition-colors hover:bg-scrim focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none [&_svg]:size-5',
        className,
      )}
    >
      {children}
    </button>
  )
}

export function AppMediaViewer({ items, index, onIndexChange }: AppMediaViewerProps) {
  const { t } = useTranslation('common')
  const open = index !== null && items[index] !== undefined
  const item = open ? items[index] : null
  const many = items.length > 1

  const go = (step: number) => {
    if (index === null) return
    onIndexChange((index + step + items.length) % items.length)
  }

  const onKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (!many) return
    if (event.key === 'ArrowRight' && item?.kind !== 'video') go(1)
    if (event.key === 'ArrowLeft' && item?.kind !== 'video') go(-1)
  }

  return (
    <DialogPrimitive.Root open={open} onOpenChange={(next) => !next && onIndexChange(null)}>
      <DialogPrimitive.Portal>
        <DialogPrimitive.Overlay className="fixed inset-0 z-50 bg-scrim/95 duration-150 data-open:animate-in data-open:fade-in-0 data-closed:animate-out data-closed:fade-out-0" />
        <DialogPrimitive.Content
          onKeyDown={onKeyDown}
          onEscapeKeyDown={(event) => {
            if (document.querySelector('[data-dismiss-guard]')) event.preventDefault()
          }}
          onClick={(event) => event.target === event.currentTarget && onIndexChange(null)}
          aria-describedby={undefined}
          className="fixed inset-0 z-50 flex items-center justify-center p-4 outline-none sm:p-16 data-open:animate-in data-open:fade-in-0 data-open:zoom-in-95"
        >
          <DialogPrimitive.Title className="sr-only">{item?.alt || t('viewer.title')}</DialogPrimitive.Title>

          {item ? (
            <figure className="flex max-h-full w-full max-w-5xl flex-col items-center justify-center gap-3">
              {item.kind === 'video' ? (
                <AppVideoPlayer key={item.id} src={item.url} poster={item.posterUrl} autoPlay className="w-full" />
              ) : (
                <img
                  key={item.id}
                  src={item.url}
                  alt={item.alt ?? item.caption ?? ''}
                  className={cn('max-w-full rounded-lg object-contain', item.caption ? 'max-h-[calc(100vh-11rem)]' : 'max-h-[calc(100vh-8rem)]')}
                />
              )}
              {item.caption ? <figcaption className="max-w-3xl text-center text-sm text-on-scrim/90">{item.caption}</figcaption> : null}
            </figure>
          ) : null}

          <ViewerButton label={t('viewer.close')} onClick={() => onIndexChange(null)} className="absolute top-4 right-4">
            <XIcon />
          </ViewerButton>

          {many && index !== null ? (
            <>
              <ViewerButton label={t('viewer.previous')} onClick={() => go(-1)} className="absolute top-4 left-4 sm:top-1/2 sm:-translate-y-1/2">
                <ChevronLeftIcon />
              </ViewerButton>
              <ViewerButton label={t('viewer.next')} onClick={() => go(1)} className="absolute top-4 left-16 sm:top-1/2 sm:right-4 sm:left-auto sm:-translate-y-1/2">
                <ChevronRightIcon />
              </ViewerButton>
              <span className="absolute top-6 left-1/2 -translate-x-1/2 rounded-full bg-scrim/70 px-3 py-1 text-sm text-on-scrim tabular-nums">
                {t('viewer.counter', { current: index + 1, total: items.length })}
              </span>
            </>
          ) : null}
        </DialogPrimitive.Content>
      </DialogPrimitive.Portal>
    </DialogPrimitive.Root>
  )
}
