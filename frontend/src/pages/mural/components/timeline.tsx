import { useEffect, useMemo, useRef, useState } from 'react'
import type { KeyboardEvent } from 'react'
import { useTranslation } from 'react-i18next'

import { cn } from '@/lib/utils'

import { decadeOf } from '../archive-date'
import type { ArchiveYearDto } from '../services/mural-service'

export type TimelineSelection = { year: number | null; decade: number | null }

type TimelineProps = {
  years: ArchiveYearDto[]
  value: TimelineSelection
  onChange: (value: TimelineSelection) => void
}

const barMaxHeight = 56

export function Timeline({ years, value, onChange }: TimelineProps) {
  const { t } = useTranslation('mural')
  const scrollerRef = useRef<HTMLDivElement>(null)
  const [hovered, setHovered] = useState<number | null>(null)

  const { decades, max } = useMemo(() => {
    if (years.length === 0) return { decades: [] as { decade: number; years: ArchiveYearDto[] }[], max: 0 }
    const counts = new Map(years.map((entry) => [entry.year, entry.count]))
    const first = decadeOf(years[0].year)
    const last = years[years.length - 1].year
    const groups: { decade: number; years: ArchiveYearDto[] }[] = []
    for (let decade = first; decade <= last; decade += 10) {
      const span: ArchiveYearDto[] = []
      for (let year = decade; year < decade + 10 && year <= last; year++) span.push({ year, count: counts.get(year) ?? 0 })
      groups.push({ decade, years: span })
    }
    return { decades: groups, max: Math.max(...years.map((entry) => entry.count)) }
  }, [years])

  const allYears = useMemo(() => decades.flatMap((group) => group.years.filter((entry) => entry.count > 0).map((entry) => entry.year)), [decades])

  useEffect(() => {
    if (value.year === null) return
    scrollerRef.current
      ?.querySelector<HTMLElement>(`[data-year="${value.year}"]`)
      ?.scrollIntoView({ behavior: 'smooth', inline: 'center', block: 'nearest' })
  }, [value.year])

  if (decades.length === 0) return null

  const onKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.key !== 'ArrowRight' && event.key !== 'ArrowLeft') return
    event.preventDefault()
    const index = value.year === null ? -1 : allYears.indexOf(value.year)
    const next = event.key === 'ArrowRight' ? Math.min(index + 1, allYears.length - 1) : Math.max(index - 1, 0)
    onChange({ year: allYears[next], decade: null })
  }

  const label = hovered ?? value.year
  const labelCount = label === null ? null : (years.find((entry) => entry.year === label)?.count ?? 0)

  return (
    <section aria-label={t('timeline.label')} className="flex flex-col gap-2 rounded-xl border bg-card p-4">
      <div className="flex items-center justify-between gap-3">
        <span className="font-heading text-sm tracking-wide text-muted-foreground uppercase" aria-live="polite">
          {label !== null ? t('timeline.yearCount', { year: label, count: labelCount ?? 0 }) : value.decade !== null ? t('timeline.decade', { decade: String(value.decade).slice(2) }) : t('timeline.hint')}
        </span>
        <button
          type="button"
          onClick={() => onChange({ year: null, decade: null })}
          aria-pressed={value.year === null && value.decade === null}
          className={cn(
            'rounded-full border px-3 py-1 text-xs font-medium transition-colors hover:bg-accent focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none',
            value.year === null && value.decade === null && 'border-primary bg-primary/15 text-primary',
          )}
        >
          {t('timeline.all')}
        </button>
      </div>

      <div
        ref={scrollerRef}
        role="listbox"
        aria-label={t('timeline.years')}
        tabIndex={0}
        onKeyDown={onKeyDown}
        className="flex gap-4 overflow-x-auto pb-1 outline-none focus-visible:ring-2 focus-visible:ring-ring/50 [scrollbar-width:thin]"
      >
        {decades.map((group) => {
          const decadeActive = value.decade === group.decade
          return (
            <div key={group.decade} className={cn('flex shrink-0 flex-col gap-1.5 rounded-lg px-1.5 pt-1 transition-colors', decadeActive && 'bg-primary/10')}>
              <div className="flex items-end gap-1" style={{ height: barMaxHeight }}>
                {group.years.map((entry) => {
                  const active = value.year === entry.year
                  const empty = entry.count === 0
                  const height = empty ? 3 : Math.max(8, Math.round((entry.count / max) * barMaxHeight))
                  return (
                    <button
                      key={entry.year}
                      type="button"
                      role="option"
                      aria-selected={active}
                      data-year={entry.year}
                      disabled={empty}
                      aria-label={t('timeline.yearCount', { year: entry.year, count: entry.count })}
                      onClick={() => onChange(active ? { year: null, decade: null } : { year: entry.year, decade: null })}
                      onPointerEnter={() => setHovered(entry.year)}
                      onPointerLeave={() => setHovered(null)}
                      className="group/bar flex h-full w-4 items-end focus-visible:outline-none disabled:cursor-default sm:w-5"
                    >
                      <span
                        className={cn(
                          'block w-full rounded-sm transition-all duration-300 ease-out',
                          empty ? 'bg-border' : 'bg-muted-foreground/40 group-hover/bar:bg-primary/70 group-focus-visible/bar:bg-primary/70',
                          active && 'bg-primary shadow-[0_0_12px] shadow-primary/50',
                          (decadeActive || active) && !empty && !active && 'bg-primary/60',
                        )}
                        style={{ height }}
                      />
                    </button>
                  )
                })}
              </div>
              <button
                type="button"
                onClick={() => onChange(decadeActive ? { year: null, decade: null } : { year: null, decade: group.decade })}
                aria-pressed={decadeActive}
                className={cn(
                  'self-start rounded px-1 font-heading text-sm tracking-wide text-muted-foreground transition-colors hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none',
                  decadeActive && 'text-primary',
                )}
              >
                {t('timeline.decadeShort', { decade: String(group.decade).slice(2) })}
              </button>
            </div>
          )
        })}
      </div>
    </section>
  )
}
