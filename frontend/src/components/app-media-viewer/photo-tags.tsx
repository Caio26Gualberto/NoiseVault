import { TagIcon, XIcon } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import type { KeyboardEvent, MouseEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { AppAvatar } from '@/components/app-avatar'
import { useAppConfirm } from '@/components/app-confirm'
import type { AppMentionOption } from '@/components/app-mention-textarea'
import { useDebouncedValue } from '@/hooks/use-debounced-value'
import { cn } from '@/lib/utils'

export type AppPhotoTag = {
  id: number
  x: number
  y: number
  name: string
  href: string | null
  canRemove: boolean
}

export type AppPhotoTagTarget = { nickname: string } | { name: string }

export type AppPhotoTagging = {
  searchPeople: (term: string) => Promise<AppMentionOption[]>
  onAdd: (itemId: string | number, x: number, y: number, target: AppPhotoTagTarget) => Promise<void>
  onRemove: (tagId: number) => Promise<void>
}

type PhotoTagsProps = {
  itemId: string | number
  url: string
  alt: string
  imageClassName: string
  tags: AppPhotoTag[]
  taggable: boolean
  tagging?: AppPhotoTagging
}

type Point = { x: number; y: number }

const searchMinLength = 2
const nameMinLength = 2

export function PhotoTags({ itemId, url, alt, imageClassName, tags, taggable, tagging }: PhotoTagsProps) {
  const { t } = useTranslation('common')
  const confirm = useAppConfirm()
  const [tagMode, setTagMode] = useState(false)
  const [showTags, setShowTags] = useState(false)
  const [point, setPoint] = useState<Point | null>(null)

  const canTag = taggable && tagging !== undefined
  const visible = showTags || tagMode

  const place = (event: MouseEvent<HTMLImageElement>) => {
    if (!tagMode) return
    const rect = event.currentTarget.getBoundingClientRect()
    const x = Math.min(1, Math.max(0, (event.clientX - rect.left) / rect.width))
    const y = Math.min(1, Math.max(0, (event.clientY - rect.top) / rect.height))
    setPoint({ x, y })
  }

  const remove = (tag: AppPhotoTag) =>
    confirm({
      title: t('viewer.tags.removeTitle'),
      message: t('viewer.tags.removeMessage', { name: tag.name }),
      confirmLabel: t('viewer.tags.remove'),
      danger: true,
      onConfirm: () => tagging?.onRemove(tag.id),
    })

  const names = tags.map((tag) => tag.name)

  return (
    <div className="flex max-w-full flex-col items-center gap-3">
      <div className="group/photo relative inline-block max-w-full">
        <img
          src={url}
          alt={alt}
          onClick={place}
          className={cn(imageClassName, tagMode && 'cursor-crosshair')}
        />
        {tags.map((tag) => (
          <span
            key={tag.id}
            style={{ left: `${tag.x * 100}%`, top: `${tag.y * 100}%` }}
            className={cn(
              'absolute flex -translate-x-1/2 flex-col items-center transition-opacity',
              visible ? 'opacity-100' : 'pointer-events-none opacity-0 group-hover/photo:pointer-events-auto group-hover/photo:opacity-100',
            )}
          >
            <span className="size-2.5 rounded-full border-2 border-on-scrim bg-primary shadow" />
            <span className="mt-1 flex items-center gap-1 rounded-md bg-scrim/85 py-0.5 pr-1 pl-2 text-xs whitespace-nowrap text-on-scrim shadow">
              {tag.href ? (
                <Link to={tag.href} className="font-medium hover:underline">
                  {tag.name}
                </Link>
              ) : (
                <span className="font-medium">{tag.name}</span>
              )}
              {tag.canRemove && tagging ? (
                <button
                  type="button"
                  onClick={() => remove(tag)}
                  aria-label={t('viewer.tags.remove')}
                  title={t('viewer.tags.remove')}
                  className="rounded p-0.5 hover:bg-on-scrim/20"
                >
                  <XIcon className="size-3" />
                </button>
              ) : (
                <span className="w-1" />
              )}
            </span>
          </span>
        ))}
        {point && tagging ? (
          <TagPicker
            point={point}
            tagging={tagging}
            onCancel={() => setPoint(null)}
            onPick={async (target) => {
              await tagging.onAdd(itemId, point.x, point.y, target)
              setPoint(null)
            }}
          />
        ) : null}
      </div>

      {canTag || tags.length > 0 ? (
        <div className="flex max-w-3xl flex-wrap items-center justify-center gap-2 text-sm text-on-scrim/90">
          {names.length > 0 ? <span>{t('viewer.tags.with', { names: names.join(', ') })}</span> : null}
          {tags.length > 0 && !tagMode ? (
            <button type="button" onClick={() => setShowTags((value) => !value)} className="rounded-full bg-scrim/70 px-3 py-1 text-xs text-on-scrim hover:bg-scrim">
              {showTags ? t('viewer.tags.hide') : t('viewer.tags.show')}
            </button>
          ) : null}
          {canTag ? (
            <button
              type="button"
              onClick={() => {
                setTagMode((value) => !value)
                setPoint(null)
              }}
              aria-pressed={tagMode}
              className={cn(
                'flex items-center gap-1.5 rounded-full px-3 py-1 text-xs',
                tagMode ? 'bg-primary text-primary-foreground' : 'bg-scrim/70 text-on-scrim hover:bg-scrim',
              )}
            >
              <TagIcon className="size-3.5" />
              {tagMode ? t('viewer.tags.done') : t('viewer.tags.start')}
            </button>
          ) : null}
          {tagMode && !point ? <span className="w-full text-center text-xs text-on-scrim/80">{t('viewer.tags.hint')}</span> : null}
        </div>
      ) : null}
    </div>
  )
}

type TagPickerProps = {
  point: Point
  tagging: AppPhotoTagging
  onCancel: () => void
  onPick: (target: AppPhotoTagTarget) => Promise<void>
}

function TagPicker({ point, tagging, onCancel, onPick }: TagPickerProps) {
  const { t } = useTranslation('common')
  const inputRef = useRef<HTMLInputElement>(null)
  const requestRef = useRef(0)
  const [query, setQuery] = useState('')
  const [active, setActive] = useState(0)
  const [busy, setBusy] = useState(false)
  const [state, setState] = useState<{ term: string; items: AppMentionOption[] }>({ term: '', items: [] })
  const term = useDebouncedValue(query.trim(), 250)
  const searchable = term.length >= searchMinLength

  useEffect(() => {
    inputRef.current?.focus()
  }, [])

  useEffect(() => {
    if (!searchable) return
    const request = ++requestRef.current
    tagging
      .searchPeople(term)
      .then((items) => {
        if (request === requestRef.current) setState({ term, items })
      })
      .catch(() => undefined)
  }, [term, searchable, tagging])

  const people = searchable && state.term === term ? state.items : []
  const freeName = query.trim()
  const options: (AppMentionOption | null)[] = [...people, ...(freeName.length >= nameMinLength ? [null] : [])]

  const choose = async (option: AppMentionOption | null) => {
    if (busy) return
    setBusy(true)
    try {
      await onPick(option ? { nickname: option.handle } : { name: freeName })
    } catch {
      setBusy(false)
    }
  }

  const handleKey = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Escape') {
      event.preventDefault()
      event.stopPropagation()
      onCancel()
      return
    }
    if (options.length === 0) return
    if (event.key === 'ArrowDown') {
      event.preventDefault()
      setActive((current) => (current + 1) % options.length)
    } else if (event.key === 'ArrowUp') {
      event.preventDefault()
      setActive((current) => (current <= 0 ? options.length - 1 : current - 1))
    } else if (event.key === 'Enter') {
      event.preventDefault()
      void choose(options[active] ?? options[0])
    }
  }

  return (
    <>
      <span
        style={{ left: `${point.x * 100}%`, top: `${point.y * 100}%` }}
        className="absolute size-12 -translate-x-1/2 -translate-y-1/2 rounded-md border-2 border-on-scrim shadow-[0_0_0_9999px] shadow-scrim/30"
      />
      <div
        data-dismiss-guard
        style={{ top: `calc(${point.y * 100}% + 1.75rem)`, ...(point.x > 0.6 ? { right: `${(1 - point.x) * 100}%` } : { left: `${point.x * 100}%` }) }}
        className={cn('absolute z-10 w-64 max-w-[80vw] overflow-hidden rounded-lg border bg-popover text-popover-foreground shadow-lg', point.x > 0.6 ? 'translate-x-6' : '-translate-x-6')}
      >
        <input
          ref={inputRef}
          value={query}
          onChange={(event) => {
            setQuery(event.target.value)
            setActive(0)
          }}
          onKeyDown={handleKey}
          placeholder={t('viewer.tags.who')}
          aria-label={t('viewer.tags.who')}
          disabled={busy}
          className="h-10 w-full border-b bg-transparent px-3 text-sm outline-none placeholder:text-muted-foreground"
        />
        {options.length > 0 ? (
          <ul role="listbox" className="flex max-h-60 flex-col overflow-y-auto py-1">
            {options.map((option, index) => (
              <li
                key={option ? option.handle : '__name'}
                role="option"
                aria-selected={index === active}
                onMouseDown={(event) => event.preventDefault()}
                onClick={() => void choose(option)}
                onMouseEnter={() => setActive(index)}
                className={cn('flex cursor-pointer items-center gap-2.5 px-3 py-2 text-sm', index === active && 'bg-accent')}
              >
                {option ? (
                  <>
                    <AppAvatar name={option.name} src={option.avatarUrl} size="sm" />
                    <span className="flex min-w-0 flex-col">
                      <span className="truncate font-medium">{option.name}</span>
                      <span className="app-meta truncate">@{option.handle}</span>
                    </span>
                  </>
                ) : (
                  <span className="truncate">{t('viewer.tags.byName', { name: freeName })}</span>
                )}
              </li>
            ))}
          </ul>
        ) : (
          <p className="px-3 py-2.5 text-xs text-muted-foreground">{t('viewer.tags.pickHint')}</p>
        )}
        <button type="button" onClick={onCancel} className="w-full border-t px-3 py-2 text-left text-xs text-muted-foreground hover:bg-accent">
          {t('actions.cancel')}
        </button>
      </div>
    </>
  )
}
