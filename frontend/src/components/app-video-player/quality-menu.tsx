import { CheckIcon, SettingsIcon } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { cn } from '@/lib/utils'

import { qualityLabel } from './use-video-source'
import type { VideoQuality } from './use-video-source'

type QualityMenuProps = {
  qualities: VideoQuality[]
  selected: number
  active: VideoQuality | null
  onSelect: (index: number) => void
  onOpenChange: (open: boolean) => void
}

const hdLines = 720

export function QualityMenu({ qualities, selected, active, onSelect, onOpenChange }: QualityMenuProps) {
  const { t } = useTranslation('common')
  const [open, setOpen] = useState(false)
  const rootRef = useRef<HTMLDivElement>(null)

  const change = (next: boolean) => {
    setOpen(next)
    onOpenChange(next)
  }

  useEffect(() => {
    if (!open) return
    const close = (event: PointerEvent) => {
      if (!rootRef.current?.contains(event.target as Node)) {
        setOpen(false)
        onOpenChange(false)
      }
    }
    const escape = (event: KeyboardEvent) => {
      if (event.key !== 'Escape') return
      setOpen(false)
      onOpenChange(false)
    }
    document.addEventListener('pointerdown', close)
    document.addEventListener('keydown', escape)
    return () => {
      document.removeEventListener('pointerdown', close)
      document.removeEventListener('keydown', escape)
    }
  }, [open, onOpenChange])

  if (qualities.length < 2) return null

  const pick = (index: number) => {
    onSelect(index)
    change(false)
  }

  const autoLabel = active ? t('player.qualityAutoWith', { quality: qualityLabel(active) }) : t('player.qualityAuto')
  const current = selected === -1 ? active : qualities.find((quality) => quality.index === selected) ?? null

  return (
    <div ref={rootRef} className="relative" data-dismiss-guard={open ? '' : undefined}>
      <button
        type="button"
        onClick={() => change(!open)}
        aria-label={t('player.quality')}
        aria-haspopup="menu"
        aria-expanded={open}
        title={t('player.quality')}
        className="relative flex size-9 items-center justify-center rounded-full text-on-scrim transition-colors hover:bg-on-scrim/15 focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none"
      >
        <SettingsIcon className={cn('size-5 transition-transform duration-300', open && 'rotate-45')} />
        {current && current.lines >= hdLines ? (
          <span className="absolute top-0.5 right-0 rounded-sm bg-primary px-0.5 text-[9px] leading-tight font-bold text-primary-foreground">
            HD
          </span>
        ) : null}
      </button>

      {open ? (
        <div
          role="menu"
          aria-label={t('player.quality')}
          className="absolute right-0 bottom-11 z-10 flex min-w-52 flex-col whitespace-nowrap gap-0.5 rounded-lg bg-scrim/95 p-1.5 shadow-lg ring-1 ring-on-scrim/10 backdrop-blur-sm"
        >
          <span className="px-2.5 pt-1 pb-1.5 font-heading text-xs tracking-wide text-on-scrim/60 uppercase">{t('player.quality')}</span>
          {qualities.map((quality) => (
            <QualityOption
              key={quality.index}
              label={qualityLabel(quality)}
              hd={quality.lines >= hdLines}
              checked={selected === quality.index}
              onSelect={() => pick(quality.index)}
            />
          ))}
          <QualityOption label={autoLabel} checked={selected === -1} onSelect={() => pick(-1)} />
        </div>
      ) : null}
    </div>
  )
}

function QualityOption({ label, hd = false, checked, onSelect }: { label: string; hd?: boolean; checked: boolean; onSelect: () => void }) {
  return (
    <button
      type="button"
      role="menuitemradio"
      aria-checked={checked}
      onClick={onSelect}
      className={cn(
        'flex items-center gap-2 rounded-md px-2.5 py-1.5 text-left text-sm text-on-scrim tabular-nums transition-colors hover:bg-on-scrim/10 focus-visible:bg-on-scrim/10 focus-visible:outline-none',
        checked && 'font-semibold',
      )}
    >
      <CheckIcon className={cn('size-4 shrink-0 text-primary', !checked && 'invisible')} />
      <span className="flex-1">{label}</span>
      {hd ? <span className="rounded-sm bg-on-scrim/15 px-1 text-[10px] font-bold">HD</span> : null}
    </button>
  )
}
