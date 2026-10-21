import { useState } from 'react'
import { Trans, useTranslation } from 'react-i18next'

import type { PagedRequest, PagedResult } from '@/api/types'
import { AppPeopleDialog } from '@/components/app-people-dialog'
import type { AppPerson } from '@/components/app-people-dialog'
import { AppReactionSummary } from '@/components/app-reaction-summary'

type LikesSummaryProps = {
  count: number
  people: AppPerson[] | undefined
  load: (query: PagedRequest) => Promise<PagedResult<AppPerson>>
  className?: string
}

export function LikesSummary({ count, people, load, className }: LikesSummaryProps) {
  const { t } = useTranslation('common')
  const [open, setOpen] = useState(false)
  const first = people?.[0]

  if (count === 0 || !first) return null

  return (
    <>
      <AppReactionSummary people={people ?? []} onOpen={() => setOpen(true)} className={className}>
        <Trans t={t} i18nKey="likes.summary" values={{ name: first.name, count: count - 1 }} count={count - 1} components={{ b: <b /> }} />
      </AppReactionSummary>
      <AppPeopleDialog open={open} onOpenChange={setOpen} title={t('likes.listTitle')} load={load} />
    </>
  )
}
