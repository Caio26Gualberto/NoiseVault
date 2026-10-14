import { useEffect, useId, useRef, useState } from 'react'
import type { ComponentProps, KeyboardEvent } from 'react'
import { useTranslation } from 'react-i18next'

import { AppAvatar } from '@/components/app-avatar'
import { AppBadge } from '@/components/app-badge'
import { useDebouncedValue } from '@/hooks/use-debounced-value'
import { cn } from '@/lib/utils'

export type AppMentionOption = {
  kind: 'person' | 'band'
  handle: string
  name: string
  avatarUrl: string | null
  verified?: boolean
}

type AppMentionTextareaProps = Omit<ComponentProps<'textarea'>, 'value' | 'onChange'> & {
  value: string
  onChange: (value: string) => void
  search: (term: string) => Promise<AppMentionOption[]>
  panelClassName?: string
}

type Token = { start: number; term: string }
type SearchState = { term: string; items: AppMentionOption[]; loading: boolean }

const tokenPattern = /(?:^|[^a-z0-9._@-])@([a-z0-9][a-z0-9._-]{0,58})$/i
const searchMinLength = 2

function tokenAt(text: string, caret: number): Token | null {
  const match = tokenPattern.exec(text.slice(0, caret))
  if (!match) return null
  return { start: caret - match[1].length - 1, term: match[1].toLowerCase() }
}

export function AppMentionTextarea({ value, onChange, search, panelClassName, className, onKeyDown, onBlur, ...props }: AppMentionTextareaProps) {
  const { t } = useTranslation()
  const listId = useId()
  const textareaRef = useRef<HTMLTextAreaElement>(null)
  const requestRef = useRef(0)
  const [token, setToken] = useState<Token | null>(null)
  const [active, setActive] = useState(0)
  const [state, setState] = useState<SearchState>({ term: '', items: [], loading: false })
  const term = useDebouncedValue(token?.term ?? '', 250)
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

  const items = token && token.term.length >= searchMinLength && state.term === term && term === token.term ? state.items : []
  const open = token !== null && token.term.length >= searchMinLength
  const pending = open && items.length === 0 && (state.loading || term !== token?.term || state.term !== term)

  const sync = (element: HTMLTextAreaElement) => {
    setToken(tokenAt(element.value, element.selectionStart ?? element.value.length))
    setActive(0)
  }

  const pick = (option: AppMentionOption) => {
    if (!token) return
    const element = textareaRef.current
    const caret = element?.selectionStart ?? value.length
    const insert = `@${option.handle} `
    const next = value.slice(0, token.start) + insert + value.slice(caret)
    onChange(next)
    setToken(null)
    const position = token.start + insert.length
    requestAnimationFrame(() => {
      element?.focus()
      element?.setSelectionRange(position, position)
    })
  }

  const handleKey = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (open && items.length > 0) {
      if (event.key === 'ArrowDown') {
        event.preventDefault()
        setActive((current) => (current + 1) % items.length)
        return
      }
      if (event.key === 'ArrowUp') {
        event.preventDefault()
        setActive((current) => (current <= 0 ? items.length - 1 : current - 1))
        return
      }
      if (event.key === 'Enter' || event.key === 'Tab') {
        event.preventDefault()
        pick(items[active] ?? items[0])
        return
      }
    }
    if (open && event.key === 'Escape') {
      event.preventDefault()
      setToken(null)
      return
    }
    onKeyDown?.(event)
  }

  return (
    <div className="relative w-full">
      <textarea
        {...props}
        ref={textareaRef}
        value={value}
        onChange={(event) => {
          onChange(event.target.value)
          sync(event.target)
        }}
        onSelect={(event) => sync(event.currentTarget)}
        onKeyDown={handleKey}
        onBlur={(event) => {
          setToken(null)
          onBlur?.(event)
        }}
        role="combobox"
        aria-expanded={open}
        aria-controls={listId}
        aria-autocomplete="list"
        aria-activedescendant={open && items.length > 0 ? `${listId}-${active}` : undefined}
        className={className}
      />
      {open ? (
        <div className={cn('absolute top-full left-0 z-40 mt-1 w-full max-w-sm overflow-hidden rounded-lg border bg-popover text-popover-foreground shadow-lg', panelClassName)}>
          {items.length > 0 ? (
            <ul id={listId} role="listbox" className="flex flex-col py-1">
              {items.map((option, index) => (
                <li
                  key={`${option.kind}-${option.handle}`}
                  id={`${listId}-${index}`}
                  role="option"
                  aria-selected={index === active}
                  onMouseDown={(event) => event.preventDefault()}
                  onClick={() => pick(option)}
                  onMouseEnter={() => setActive(index)}
                  className={cn('flex cursor-pointer items-center gap-3 px-3 py-2', index === active && 'bg-accent')}
                >
                  <AppAvatar name={option.name} src={option.avatarUrl} size="sm" verified={option.verified} />
                  <span className="flex min-w-0 flex-1 flex-col">
                    <span className="truncate text-sm font-medium">{option.name}</span>
                    <span className="app-meta truncate">@{option.handle}</span>
                  </span>
                  {option.kind === 'band' ? <AppBadge tone="brand">{t('mentions.band')}</AppBadge> : null}
                </li>
              ))}
            </ul>
          ) : (
            <p id={listId} className="px-3 py-3 text-sm text-muted-foreground">
              {pending ? t('mentions.loading') : t('mentions.empty')}
            </p>
          )}
        </div>
      ) : null}
    </div>
  )
}
