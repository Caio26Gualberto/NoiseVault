import { useEffect, useEffectEvent, useState } from 'react'
import { Link } from 'react-router-dom'

import { emptyPage } from '@/api/types'
import type { PagedRequest, PagedResult } from '@/api/types'
import { AppAvatar } from '@/components/app-avatar'
import { AppDialog } from '@/components/app-dialog'
import { AppPagination } from '@/components/app-pagination'

export type AppPerson = {
  id: number
  name: string
  nickname: string
  avatarUrl: string | null
}

const pageSize = 30

type AppPeopleDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  load: (query: PagedRequest) => Promise<PagedResult<AppPerson>>
}

export function AppPeopleDialog({ open, onOpenChange, title, load }: AppPeopleDialogProps) {
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<AppPerson>>(() => emptyPage(pageSize))
  const fetchPage = useEffectEvent((target: number) => load({ page: target, pageSize }))

  useEffect(() => {
    if (!open) return
    let cancelled = false
    fetchPage(page)
      .then((data) => {
        if (!cancelled) setResult((previous) => (page === 1 ? data : { ...data, items: [...previous.items, ...data.items] }))
      })
      .catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [open, page])

  const change = (next: boolean) => {
    if (!next) setPage(1)
    onOpenChange(next)
  }

  return (
    <AppDialog open={open} onOpenChange={change} title={title}>
      <ul className="flex max-h-[60vh] flex-col gap-1 overflow-y-auto">
        {result.items.map((person) => (
          <li key={person.id}>
            <Link to={`/perfil/${person.nickname}`} onClick={() => change(false)} className="flex items-center gap-3 rounded-lg p-2 transition-colors hover:bg-accent">
              <AppAvatar name={person.name} src={person.avatarUrl} size="md" />
              <span className="flex min-w-0 flex-col">
                <span className="truncate text-sm font-medium">{person.name}</span>
                <span className="app-meta">@{person.nickname}</span>
              </span>
            </Link>
          </li>
        ))}
      </ul>
      <AppPagination page={page} totalPages={result.totalPages} totalCount={result.totalCount} onChange={setPage} mode="loadMore" />
    </AppDialog>
  )
}
