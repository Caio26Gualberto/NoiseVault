import { useTranslation } from 'react-i18next'

import type { AppBadgeTone } from '@/components/app-badge'

export const JobState = {
  succeeded: 'Succeeded',
  failed: 'Failed',
  processing: 'Processing',
  enqueued: 'Enqueued',
  scheduled: 'Scheduled',
  deleted: 'Deleted',
  awaiting: 'Awaiting',
} as const

export type JobState = (typeof JobState)[keyof typeof JobState]

const toneByState: Record<JobState, AppBadgeTone> = {
  [JobState.succeeded]: 'success',
  [JobState.failed]: 'danger',
  [JobState.processing]: 'info',
  [JobState.enqueued]: 'info',
  [JobState.scheduled]: 'warning',
  [JobState.deleted]: 'neutral',
  [JobState.awaiting]: 'neutral',
}

const keyByState: Record<JobState, string> = {
  [JobState.succeeded]: 'succeeded',
  [JobState.failed]: 'failed',
  [JobState.processing]: 'processing',
  [JobState.enqueued]: 'enqueued',
  [JobState.scheduled]: 'scheduled',
  [JobState.deleted]: 'deleted',
  [JobState.awaiting]: 'awaiting',
}

function isJobState(value: string): value is JobState {
  return value in toneByState
}

export function isRunning(state: string | null | undefined): boolean {
  return state === JobState.enqueued || state === JobState.processing
}

export function jobStateTone(state: string): AppBadgeTone {
  return isJobState(state) ? toneByState[state] : 'neutral'
}

export function useJobLabels() {
  const { t } = useTranslation('jobs')

  return {
    state: (state: string) => (isJobState(state) ? t(`state.${keyByState[state]}`) : state),
    duration: (ms: number) => {
      if (ms < 1000) return t('duration.ms', { value: ms })
      if (ms < 60000) return t('duration.seconds', { value: (ms / 1000).toLocaleString('pt-BR', { maximumFractionDigits: 1 }) })
      return t('duration.minutes', { minutes: Math.floor(ms / 60000), seconds: Math.round((ms % 60000) / 1000) })
    },
  }
}

export function prettyResult(raw: string): string {
  try {
    return JSON.stringify(JSON.parse(raw), null, 2)
  } catch {
    return raw
  }
}
