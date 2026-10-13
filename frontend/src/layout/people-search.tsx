import { SearchIcon } from 'lucide-react'
import { useEffect, useId, useRef, useState } from 'react'
import type { KeyboardEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'

import { AppAvatar } from '@/components/app-avatar'
import { cn } from '@/lib/utils'
import { peopleSearchMinLength, searchPeople } from '@/pages/perfil/services/perfil-service'
import type { ProfileSummaryDto } from '@/pages/perfil/services/perfil-service'

import { useDebouncedValue } from '../hooks/use-debounced-value'

type SearchState = { term: string; items: ProfileSummaryDto[]; loading: boolean }

export function PeopleSearch() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const listId = useId()
  const requestRef = useRef(0)
  const [query, setQuery] = useState('')
  const [open, setOpen] = useState(false)
  const [active, setActive] = useState(-1)
  const [state, setState] = useState<SearchState>({ term: '', items: [], loading: false })
  const term = useDebouncedValue(query.trim(), 350)
  const searchable = term.length >= peopleSearchMinLength

  useEffect(() => {
    if (!searchable) return
    const request = ++requestRef.current
    setState((current) => ({ ...current, loading: true }))
    searchPeople(term)
      .then((result) => {
        if (request === requestRef.current) setState({ term, items: result.items, loading: false })
      })
      .catch(() => {
        if (request === requestRef.current) setState({ term, items: [], loading: false })
      })
  }, [term, searchable])

  const items = searchable && state.term === term ? state.items : []
  const showPanel = open && query.trim().length >= peopleSearchMinLength

  const go = (person: ProfileSummaryDto) => {
    navigate(`/perfil/${person.nickname}`)
    setQuery('')
    setOpen(false)
    setActive(-1)
  }

  const handleKey = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Escape') {
      setOpen(false)
      setActive(-1)
      return
    }
    if (items.length === 0) return
    if (event.key === 'ArrowDown') {
      event.preventDefault()
      setOpen(true)
      setActive((current) => (current + 1) % items.length)
    } else if (event.key === 'ArrowUp') {
      event.preventDefault()
      setActive((current) => (current <= 0 ? items.length - 1 : current - 1))
    } else if (event.key === 'Enter') {
      event.preventDefault()
      go(items[active >= 0 ? active : 0])
    }
  }

  const pending = state.loading || (searchable && state.term !== term) || query.trim() !== term

  return (
    <div className="relative ml-auto hidden w-full max-w-sm md:block">
      <SearchIcon className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
      <input
        type="search"
        role="combobox"
        value={query}
        onChange={(event) => {
          setQuery(event.target.value)
          setOpen(true)
          setActive(-1)
        }}
        onFocus={() => setOpen(true)}
        onBlur={() => setOpen(false)}
        onKeyDown={handleKey}
        placeholder={t('layout:search.placeholder')}
        aria-label={t('layout:search.placeholder')}
        aria-expanded={showPanel}
        aria-controls={listId}
        aria-autocomplete="list"
        aria-activedescendant={active >= 0 ? `${listId}-${active}` : undefined}
        autoComplete="off"
        className="h-9 w-full rounded-lg border bg-muted/40 pl-9 pr-3 text-sm text-foreground placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
      />
      {showPanel ? (
        <div className="absolute inset-x-0 top-full z-40 mt-1.5 overflow-hidden rounded-lg border bg-popover text-popover-foreground shadow-lg">
          {items.length > 0 ? (
            <ul id={listId} role="listbox" className="flex flex-col py-1">
              {items.map((person, index) => (
                <li
                  key={person.id}
                  id={`${listId}-${index}`}
                  role="option"
                  aria-selected={index === active}
                  onMouseDown={(event) => event.preventDefault()}
                  onClick={() => go(person)}
                  onMouseEnter={() => setActive(index)}
                  className={cn('flex cursor-pointer items-center gap-3 px-3 py-2', index === active && 'bg-accent')}
                >
                  <AppAvatar name={person.name} src={person.avatarUrl} size="sm" />
                  <span className="flex min-w-0 flex-col">
                    <span className="truncate text-sm font-medium">{person.name}</span>
                    <span className="app-meta truncate">@{person.nickname}</span>
                  </span>
                </li>
              ))}
            </ul>
          ) : (
            <p id={listId} className="px-3 py-3 text-sm text-muted-foreground">
              {pending ? t('layout:search.loading') : t('layout:search.empty')}
            </p>
          )}
        </div>
      ) : null}
    </div>
  )
}
