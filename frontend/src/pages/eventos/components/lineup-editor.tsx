import { ArrowDownIcon, ArrowUpIcon, PlusIcon, SearchIcon, XIcon } from 'lucide-react'
import { useEffect, useState } from 'react'
import type { KeyboardEvent } from 'react'
import { useTranslation } from 'react-i18next'

import { AppAvatar } from '@/components/app-avatar'
import { AppIconButton } from '@/components/app-button'
import { useDebouncedValue } from '@/hooks/use-debounced-value'
import { bandPagePath } from '@/pages/bandas/band-kind'
import { bandSearchMinLength, searchBands } from '@/pages/bandas/services/bandas-service'
import type { BandSummaryDto } from '@/pages/bandas/services/bandas-service'

export type LineupItem = {
  key: string
  band: BandSummaryDto | null
  name: string
}

export const lineupMax = 30
const nameMaxLength = 100

type LineupEditorProps = {
  value: LineupItem[]
  onChange: (value: LineupItem[]) => void
  legend?: string
  hint?: string
}

export function LineupEditor({ value, onChange, legend, hint }: LineupEditorProps) {
  const { t } = useTranslation('eventos')
  const [query, setQuery] = useState('')
  const term = useDebouncedValue(query.trim())
  const [suggestions, setSuggestions] = useState<BandSummaryDto[]>([])
  const full = value.length >= lineupMax

  useEffect(() => {
    if (term.length < bandSearchMinLength) return
    let cancelled = false
    searchBands(term)
      .then((page) => {
        if (!cancelled) setSuggestions(page.items)
      })
      .catch(() => {
        if (!cancelled) setSuggestions([])
      })
    return () => {
      cancelled = true
    }
  }, [term])

  const taken = new Set(value.filter((item) => item.band).map((item) => item.band!.id))
  const visible = query.trim().length >= bandSearchMinLength ? suggestions.filter((band) => !taken.has(band.id)) : []
  const freeName = query.trim().slice(0, nameMaxLength)

  const add = (item: Omit<LineupItem, 'key'>) => {
    if (full) return
    onChange([...value, { ...item, key: crypto.randomUUID() }])
    setQuery('')
    setSuggestions([])
  }

  const move = (index: number, step: number) => {
    const next = [...value]
    const [item] = next.splice(index, 1)
    next.splice(index + step, 0, item)
    onChange(next)
  }

  const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key !== 'Enter') return
    event.preventDefault()
    if (freeName) add({ band: null, name: freeName })
  }

  return (
    <fieldset className="flex flex-col gap-2">
      <legend className="text-sm font-medium">{legend ?? t('form.fields.lineup')}</legend>
      <p className="app-meta">{hint ?? t('form.lineupHint')}</p>

      {value.length > 0 ? (
        <ol className="flex flex-col gap-1.5">
          {value.map((item, index) => (
            <li key={item.key} className="flex items-center gap-2 rounded-lg border bg-background px-2 py-1.5">
              <span className="w-5 text-center text-xs text-muted-foreground tabular-nums">{index + 1}</span>
              {item.band ? (
                <AppAvatar name={item.band.name} src={item.band.avatarUrl} size="sm" verified={item.band.isVerified} />
              ) : (
                <span className="flex size-7 items-center justify-center rounded-full border border-dashed text-xs text-muted-foreground">?</span>
              )}
              <span className="flex min-w-0 flex-1 flex-col">
                <span className="truncate text-sm font-medium">{item.band?.name ?? item.name}</span>
                <span className="app-meta">{item.band ? t('form.lineupLinked') : t('form.lineupFree')}</span>
              </span>
              <AppIconButton type="button" label={t('form.lineupUp')} size="sm" disabled={index === 0} onClick={() => move(index, -1)}>
                <ArrowUpIcon />
              </AppIconButton>
              <AppIconButton type="button" label={t('form.lineupDown')} size="sm" disabled={index === value.length - 1} onClick={() => move(index, 1)}>
                <ArrowDownIcon />
              </AppIconButton>
              <AppIconButton type="button" label={t('form.lineupRemove')} size="sm" onClick={() => onChange(value.filter((other) => other.key !== item.key))}>
                <XIcon />
              </AppIconButton>
            </li>
          ))}
        </ol>
      ) : null}

      <div className="relative">
        <SearchIcon className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
        <input
          type="text"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          onKeyDown={onKeyDown}
          disabled={full}
          maxLength={nameMaxLength}
          placeholder={full ? t('form.lineupFull', { max: lineupMax }) : t('form.lineupPlaceholder')}
          aria-label={t('form.lineupPlaceholder')}
          className="h-10 w-full rounded-lg border bg-transparent pr-3 pl-9 text-sm placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none disabled:opacity-60"
        />
      </div>

      {freeName ? (
        <ul className="flex flex-col overflow-hidden rounded-lg border">
          {visible.map((band) => (
            <li key={band.id}>
              <button
                type="button"
                onClick={() => add({ band, name: band.name })}
                className="flex w-full items-center gap-3 px-3 py-2 text-left text-sm transition-colors hover:bg-accent focus-visible:bg-accent focus-visible:outline-none"
              >
                <AppAvatar name={band.name} src={band.avatarUrl} size="sm" verified={band.isVerified} />
                <span className="flex-1 truncate font-medium">{band.name}</span>
                <span className="app-meta">{bandPagePath(band)}</span>
              </button>
            </li>
          ))}
          <li>
            <button
              type="button"
              onClick={() => add({ band: null, name: freeName })}
              className="flex w-full items-center gap-3 border-t px-3 py-2 text-left text-sm transition-colors first:border-t-0 hover:bg-accent focus-visible:bg-accent focus-visible:outline-none"
            >
              <PlusIcon className="size-4 text-primary" />
              <span>{t('form.lineupAddFree', { name: freeName })}</span>
            </button>
          </li>
        </ul>
      ) : null}
    </fieldset>
  )
}
