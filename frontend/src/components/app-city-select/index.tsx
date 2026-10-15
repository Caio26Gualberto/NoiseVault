import { CheckIcon, ChevronDownIcon, GlobeIcon, LoaderCircleIcon, SearchIcon, XIcon } from 'lucide-react'
import { useEffect, useId, useRef, useState } from 'react'
import type { KeyboardEvent } from 'react'
import { useTranslation } from 'react-i18next'

import { AppButton } from '@/components/app-button'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { useDebouncedValue } from '@/hooks/use-debounced-value'
import { cn } from '@/lib/utils'

export type AppCity = { id: number; name: string; uf: string }
export type AppCityValue = { city: AppCity | null; cityName: string }

const searchMinLength = 2
const abroadMinLength = 2
const abroadMaxLength = 80

type AppCitySelectProps = {
  value: AppCityValue
  onChange: (value: AppCityValue) => void
  search: (term: string) => Promise<AppCity[]>
  suggest?: () => Promise<AppCity[]>
  placeholder: string
  clearLabel?: string
  allowAbroad?: boolean
  id?: string
  invalid?: boolean
  describedBy?: string
  ariaLabel?: string
  className?: string
}

type SearchState = { term: string; items: AppCity[]; loading: boolean }

export function AppCitySelect({
  value,
  onChange,
  search,
  suggest,
  placeholder,
  clearLabel,
  allowAbroad = false,
  id,
  invalid,
  describedBy,
  ariaLabel,
  className,
}: AppCitySelectProps) {
  const { t } = useTranslation()
  const listId = useId()
  const requestRef = useRef(0)
  const [open, setOpen] = useState(false)
  const [abroad, setAbroad] = useState(false)
  const [query, setQuery] = useState('')
  const [abroadText, setAbroadText] = useState('')
  const [active, setActive] = useState(-1)
  const [suggestions, setSuggestions] = useState<AppCity[]>([])
  const [state, setState] = useState<SearchState>({ term: '', items: [], loading: false })
  const term = useDebouncedValue(query.trim(), 300)
  const searchable = term.length >= searchMinLength

  useEffect(() => {
    if (!searchable) return
    const request = ++requestRef.current
    setState((current) => ({ ...current, loading: true }))
    search(term)
      .then((items) => {
        if (request === requestRef.current) setState({ term, items, loading: false })
      })
      .catch(() => {
        if (request === requestRef.current) setState({ term, items: [], loading: false })
      })
  }, [term, searchable, search])

  const showingSuggestions = query.trim().length < searchMinLength && suggestions.length > 0
  const items = showingSuggestions ? suggestions : searchable && state.term === term ? state.items : []
  const pending = state.loading || (searchable && state.term !== term) || query.trim() !== term
  const abroadName = abroadText.trim()
  const label = value.city ? `${value.city.name}, ${value.city.uf}` : value.cityName.trim() || null

  const changeOpen = (next: boolean) => {
    setOpen(next)
    if (!next) return
    if (suggest && suggestions.length === 0) {
      suggest()
        .then(setSuggestions)
        .catch(() => undefined)
    }
    setQuery('')
    setActive(-1)
    setAbroad(!value.city && value.cityName.trim().length > 0)
    setAbroadText(value.city ? '' : value.cityName)
  }

  const pick = (city: AppCity) => {
    onChange({ city, cityName: '' })
    setOpen(false)
  }

  const clear = () => {
    onChange({ city: null, cityName: '' })
    setOpen(false)
  }

  const confirmAbroad = () => {
    if (abroadName.length < abroadMinLength) return
    onChange({ city: null, cityName: abroadName })
    setOpen(false)
  }

  const handleKey = (event: KeyboardEvent<HTMLInputElement>) => {
    if (items.length === 0) return
    if (event.key === 'ArrowDown') {
      event.preventDefault()
      setActive((current) => (current + 1) % items.length)
    } else if (event.key === 'ArrowUp') {
      event.preventDefault()
      setActive((current) => (current <= 0 ? items.length - 1 : current - 1))
    } else if (event.key === 'Enter') {
      event.preventDefault()
      pick(items[active >= 0 ? active : 0])
    }
  }

  return (
    <Popover open={open} onOpenChange={changeOpen}>
      <PopoverTrigger asChild>
        <button
          type="button"
          id={id}
          role="combobox"
          aria-expanded={open}
          aria-invalid={invalid || undefined}
          aria-describedby={describedBy}
          aria-label={ariaLabel}
          className={cn(
            'flex h-11 w-full items-center justify-between gap-1.5 rounded-lg border border-input bg-transparent py-2 pr-2 pl-2.5 text-left text-[15px] whitespace-nowrap transition-colors outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 aria-invalid:border-destructive aria-invalid:ring-3 aria-invalid:ring-destructive/20 md:h-10 dark:bg-input/30 dark:hover:bg-input/50',
            className,
          )}
        >
          <span className={cn('flex min-w-0 items-center gap-1.5', !label && 'text-muted-foreground')}>
            {label && !value.city ? <GlobeIcon className="size-4 shrink-0 text-muted-foreground" /> : null}
            <span className="truncate">{label ?? placeholder}</span>
          </span>
          <ChevronDownIcon className="size-4 shrink-0 text-muted-foreground" />
        </button>
      </PopoverTrigger>
      <PopoverContent align="start" className="w-(--radix-popover-trigger-width) min-w-64 gap-0 p-0">
        {abroad ? (
          <div className="flex flex-col gap-3 p-3">
            <label className="flex flex-col gap-1.5 text-sm font-medium">
              {t('city.abroadLabel')}
              <input
                autoFocus
                value={abroadText}
                maxLength={abroadMaxLength}
                onChange={(event) => setAbroadText(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter') {
                    event.preventDefault()
                    confirmAbroad()
                  }
                }}
                placeholder={t('city.abroadPlaceholder')}
                className="h-10 w-full rounded-md border bg-transparent px-2.5 text-sm font-normal placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
              />
            </label>
            <div className="flex justify-between gap-2">
              <AppButton type="button" intent="ghost" size="sm" onClick={() => setAbroad(false)}>
                {t('city.backToBrazil')}
              </AppButton>
              <AppButton type="button" size="sm" disabled={abroadName.length < abroadMinLength} onClick={confirmAbroad}>
                {t('city.abroadUse')}
              </AppButton>
            </div>
          </div>
        ) : (
          <>
            <div className="relative border-b">
              <SearchIcon className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
              <input
                autoFocus
                type="search"
                role="searchbox"
                value={query}
                onChange={(event) => {
                  setQuery(event.target.value)
                  setActive(-1)
                }}
                onKeyDown={handleKey}
                placeholder={t('city.searchPlaceholder')}
                aria-label={t('city.searchPlaceholder')}
                aria-controls={listId}
                aria-activedescendant={active >= 0 ? `${listId}-${active}` : undefined}
                autoComplete="off"
                className="h-10 w-full bg-transparent pr-3 pl-9 text-sm placeholder:text-muted-foreground focus-visible:outline-none"
              />
            </div>
            <div className="max-h-64 overflow-y-auto py-1">
              {query.trim().length < searchMinLength && !showingSuggestions ? (
                <p className="px-3 py-2.5 text-sm text-muted-foreground">{t('city.typeMore')}</p>
              ) : items.length > 0 ? (
                <ul id={listId} role="listbox" aria-label={showingSuggestions ? t('city.suggestions') : undefined} className="flex flex-col">
                  {showingSuggestions ? (
                    <li role="presentation" className="px-3 pt-1.5 pb-1 text-xs font-medium tracking-wide text-muted-foreground uppercase">
                      {t('city.suggestions')}
                    </li>
                  ) : null}
                  {items.map((city, index) => {
                    const selected = value.city?.id === city.id
                    return (
                      <li
                        key={city.id}
                        id={`${listId}-${index}`}
                        role="option"
                        aria-selected={selected}
                        onClick={() => pick(city)}
                        onMouseEnter={() => setActive(index)}
                        className={cn('flex cursor-pointer items-center justify-between gap-2 px-3 py-2 text-sm', index === active && 'bg-accent')}
                      >
                        <span className="truncate">
                          {city.name} <span className="text-muted-foreground">{city.uf}</span>
                        </span>
                        {selected ? <CheckIcon className="size-4 shrink-0 text-primary" /> : null}
                      </li>
                    )
                  })}
                </ul>
              ) : (
                <p id={listId} className="flex items-center gap-2 px-3 py-2.5 text-sm text-muted-foreground">
                  {pending ? <LoaderCircleIcon className="size-4 animate-spin" /> : null}
                  {pending ? t('city.loading') : t('city.empty')}
                </p>
              )}
            </div>
            {allowAbroad || (clearLabel && label) ? (
              <div className="flex flex-col border-t py-1">
                {allowAbroad ? (
                  <button
                    type="button"
                    onClick={() => setAbroad(true)}
                    className="flex items-center gap-2 px-3 py-2 text-left text-sm hover:bg-accent focus-visible:bg-accent focus-visible:outline-none"
                  >
                    <GlobeIcon className="size-4 text-muted-foreground" />
                    {t('city.abroad')}
                  </button>
                ) : null}
                {clearLabel && label ? (
                  <button
                    type="button"
                    onClick={clear}
                    className="flex items-center gap-2 px-3 py-2 text-left text-sm text-muted-foreground hover:bg-accent focus-visible:bg-accent focus-visible:outline-none"
                  >
                    <XIcon className="size-4" />
                    {clearLabel}
                  </button>
                ) : null}
              </div>
            ) : null}
          </>
        )}
      </PopoverContent>
    </Popover>
  )
}
