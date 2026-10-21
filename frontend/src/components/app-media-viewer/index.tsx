import { ChevronLeftIcon, ChevronRightIcon, RotateCwIcon, XIcon } from 'lucide-react'
import { Dialog as DialogPrimitive } from 'radix-ui'
import { useState } from 'react'
import type { KeyboardEvent, ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import { AppVideoPlayer } from '@/components/app-video-player'
import { cn } from '@/lib/utils'

import { PhotoTags } from './photo-tags'
import type { AppPhotoTag, AppPhotoTagging } from './photo-tags'

export type { AppPhotoTag, AppPhotoTagTarget, AppPhotoTagging } from './photo-tags'

export type ViewerItem = {
  id: string | number
  kind: 'photo' | 'video'
  url: string
  posterUrl?: string | null
  alt?: string
  caption?: string | null
  tags?: AppPhotoTag[]
  taggable?: boolean
}

type AppMediaViewerProps = {
  items: ViewerItem[]
  index: number | null
  onIndexChange: (index: number | null) => void
  tagging?: AppPhotoTagging
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

export function AppMediaViewer({ items, index, onIndexChange, tagging }: AppMediaViewerProps) {
  const { t } = useTranslation('common')
  const open = index !== null && items[index] !== undefined
  const item = open ? items[index] : null
  const many = items.length > 1
  const [turn, setTurn] = useState<{ id: string | number; degrees: number } | null>(null)
  const degrees = item && turn?.id === item.id ? turn.degrees : 0
  const sideways = degrees % 180 !== 0

  const close = () => {
    setTurn(null)
    onIndexChange(null)
  }

  const rotate = () => {
    if (!item || item.kind === 'video') return
    setTurn({ id: item.id, degrees: (degrees + 90) % 360 })
  }

  const go = (step: number) => {
    if (index === null) return
    onIndexChange((index + step + items.length) % items.length)
  }

  const onKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.target instanceof HTMLInputElement) return
    if (event.key === 'r' || event.key === 'R') rotate()
    if (!many) return
    if (event.key === 'ArrowRight' && item?.kind !== 'video') go(1)
    if (event.key === 'ArrowLeft' && item?.kind !== 'video') go(-1)
  }

  return (
    <DialogPrimitive.Root open={open} onOpenChange={(next) => !next && close()}>
      <DialogPrimitive.Portal>
        <DialogPrimitive.Overlay className="fixed inset-0 z-50 bg-scrim/95 duration-150 data-open:animate-in data-open:fade-in-0 data-closed:animate-out data-closed:fade-out-0" />
        <DialogPrimitive.Content
          onKeyDown={onKeyDown}
          onEscapeKeyDown={(event) => {
            if (document.querySelector('[data-dismiss-guard]')) event.preventDefault()
          }}
          onClick={(event) => event.target === event.currentTarget && close()}
          aria-describedby={undefined}
          className="fixed inset-0 z-50 flex items-center justify-center p-4 outline-none sm:p-16 data-open:animate-in data-open:fade-in-0 data-open:zoom-in-95"
        >
          <DialogPrimitive.Title className="sr-only">{item?.alt || t('viewer.title')}</DialogPrimitive.Title>

          {item ? (
            <figure className="flex max-h-full w-full max-w-5xl flex-col items-center justify-center gap-3">
              {item.kind === 'video' ? (
                <AppVideoPlayer key={item.id} src={item.url} poster={item.posterUrl} autoPlay className="w-full" />
              ) : degrees !== 0 ? (
                <img
                  key={`${item.id}-${degrees}`}
                  src={item.url}
                  alt={item.alt ?? item.caption ?? ''}
                  style={{ transform: `rotate(${degrees}deg)` }}
                  className={cn(
                    'rounded-lg object-contain',
                    sideways ? 'max-h-[calc(100vw-2rem)] max-w-[calc(100vh-10rem)] sm:max-h-[calc(100vw-8rem)]' : 'max-h-[calc(100vh-8rem)] max-w-full',
                  )}
                />
              ) : (
                <PhotoTags
                  key={item.id}
                  itemId={item.id}
                  url={item.url}
                  alt={item.alt ?? item.caption ?? ''}
                  imageClassName={cn('max-w-full rounded-lg object-contain', item.caption || item.tags?.length || item.taggable ? 'max-h-[calc(100vh-13rem)]' : 'max-h-[calc(100vh-8rem)]')}
                  tags={item.tags ?? []}
                  taggable={item.taggable ?? false}
                  tagging={tagging}
                />
              )}
              {item.caption ? <figcaption className="max-w-3xl text-center text-sm text-on-scrim/90">{item.caption}</figcaption> : null}
            </figure>
          ) : null}

          <ViewerButton label={t('viewer.close')} onClick={close} className="absolute top-[calc(1rem+env(safe-area-inset-top))] right-4">
            <XIcon />
          </ViewerButton>

          {item && item.kind === 'photo' ? (
            <ViewerButton label={t('viewer.rotate')} onClick={rotate} className="absolute top-[calc(1rem+env(safe-area-inset-top))] right-16">
              <RotateCwIcon />
            </ViewerButton>
          ) : null}

          {many && index !== null ? (
            <>
              <ViewerButton label={t('viewer.previous')} onClick={() => go(-1)} className="absolute top-[calc(1rem+env(safe-area-inset-top))] left-4 sm:top-1/2 sm:-translate-y-1/2">
                <ChevronLeftIcon />
              </ViewerButton>
              <ViewerButton label={t('viewer.next')} onClick={() => go(1)} className="absolute top-[calc(1rem+env(safe-area-inset-top))] left-16 sm:top-1/2 sm:right-4 sm:left-auto sm:-translate-y-1/2">
                <ChevronRightIcon />
              </ViewerButton>
              <span className="absolute top-[calc(1.5rem+env(safe-area-inset-top))] left-1/2 -translate-x-1/2 rounded-full bg-scrim/70 px-3 py-1 text-sm text-on-scrim tabular-nums">
                {t('viewer.counter', { current: index + 1, total: items.length })}
              </span>
            </>
          ) : null}
        </DialogPrimitive.Content>
      </DialogPrimitive.Portal>
    </DialogPrimitive.Root>
  )
}
