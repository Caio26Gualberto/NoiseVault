import { useState } from 'react'
import { Trans, useTranslation } from 'react-i18next'

import { AppPeopleDialog } from '@/components/app-people-dialog'
import { AppReactionSummary } from '@/components/app-reaction-summary'

import { getEventInterested } from '../services/eventos-service'
import type { EventDto } from '../services/eventos-service'

type GoingSummaryProps = {
  event: EventDto
}

export function GoingSummary({ event }: GoingSummaryProps) {
  const { t } = useTranslation('eventos')
  const [open, setOpen] = useState(false)
  const first = event.interestedPreview[0]

  if (event.interestedCount === 0 || !first) return null

  const others = event.interestedCount - 1

  return (
    <>
      <AppReactionSummary people={event.interestedPreview} onOpen={() => setOpen(true)}>
        <Trans t={t} i18nKey="goingSummary" values={{ name: first.name, count: others }} count={others} components={{ b: <b /> }} />
      </AppReactionSummary>
      <AppPeopleDialog open={open} onOpenChange={setOpen} title={t('goingTitle')} load={(query) => getEventInterested(event.id, query)} />
    </>
  )
}
