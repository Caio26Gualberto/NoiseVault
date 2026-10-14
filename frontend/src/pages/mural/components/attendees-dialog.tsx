import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { AppAvatar } from '@/components/app-avatar'
import { AppDialog } from '@/components/app-dialog'
import { AppPagination } from '@/components/app-pagination'
import type { ProfileSummaryDto } from '@/pages/perfil/services/perfil-service'

import { getArchiveAttendees } from '../services/mural-service'

const pageSize = 30

type AttendeesDialogProps = {
  entryId: number
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function AttendeesDialog({ entryId, open, onOpenChange }: AttendeesDialogProps) {
  const { t } = useTranslation('mural')
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<ProfileSummaryDto>>(() => emptyPage(pageSize))

  useEffect(() => {
    if (!open) return
    let cancelled = false
    getArchiveAttendees(entryId, { page, pageSize })
      .then((data) => {
        if (!cancelled) setResult((previous) => (page === 1 ? data : { ...data, items: [...previous.items, ...data.items] }))
      })
      .catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [entryId, open, page])

  return (
    <AppDialog open={open} onOpenChange={onOpenChange} title={t('attendance.listTitle', { count: result.totalCount })}>
      <ul className="flex max-h-[60vh] flex-col gap-1 overflow-y-auto">
        {result.items.map((person) => (
          <li key={person.id}>
            <Link to={`/perfil/${person.nickname}`} className="flex items-center gap-3 rounded-lg p-2 transition-colors hover:bg-accent">
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
