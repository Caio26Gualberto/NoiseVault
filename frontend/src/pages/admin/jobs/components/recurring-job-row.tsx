import { AlertTriangleIcon, PlayIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { AppBadge } from '@/components/app-badge'
import { AppButton } from '@/components/app-button'
import { formatDateTime, formatRelative } from '@/lib/format'

import { isRunning, jobStateTone, prettyResult, useJobLabels } from '../job-state'
import type { RecurringJobDto } from '../services/jobs-admin-service'

type RecurringJobRowProps = {
  job: RecurringJobDto
  canManage: boolean
  triggering: boolean
  onTrigger: (job: RecurringJobDto) => void
}

export function RecurringJobRow({ job, canManage, triggering, onTrigger }: RecurringJobRowProps) {
  const { t } = useTranslation('jobs')
  const labels = useJobLabels()
  const running = isRunning(job.lastJobState)
  const failed = Boolean(job.lastError) && !running

  return (
    <li className="flex flex-col gap-3 p-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div className="flex min-w-0 flex-col gap-1">
          <p className="font-medium">{job.name ?? job.id}</p>
          <p className="app-meta flex flex-wrap items-center gap-x-2 gap-y-1">
            <code className="font-mono text-xs">{job.id}</code>
            {job.queue ? <span>· {t('queue', { queue: job.queue })}</span> : null}
            {job.cron ? <code className="font-mono text-xs">· {job.cron}</code> : null}
          </p>
        </div>
        {canManage && job.canTrigger ? (
          <AppButton
            intent="neutral"
            size="sm"
            loading={triggering}
            disabled={running || triggering}
            onClick={() => onTrigger(job)}
            className="self-start"
          >
            <PlayIcon />
            {running ? t('running') : t('trigger')}
          </AppButton>
        ) : null}
      </div>

      <dl className="grid gap-x-6 gap-y-2 text-sm sm:grid-cols-3">
        <div className="flex flex-col gap-1">
          <dt className="app-meta">{t('lastRun')}</dt>
          <dd className="flex flex-wrap items-center gap-2">
            {job.lastJobState ? <AppBadge tone={jobStateTone(job.lastJobState)}>{labels.state(job.lastJobState)}</AppBadge> : null}
            {job.lastExecution ? (
              <span title={formatDateTime(job.lastExecution)}>{formatRelative(job.lastExecution)}</span>
            ) : (
              <span className="text-muted-foreground">{t('never')}</span>
            )}
          </dd>
        </div>
        <div className="flex flex-col gap-1">
          <dt className="app-meta">{t('duration.label')}</dt>
          <dd className="tabular-nums">{job.lastDurationMs != null ? labels.duration(job.lastDurationMs) : t('none')}</dd>
        </div>
        <div className="flex flex-col gap-1">
          <dt className="app-meta">{t('nextRun')}</dt>
          <dd>{job.nextExecution ? <span title={formatDateTime(job.nextExecution)}>{formatRelative(job.nextExecution)}</span> : t('none')}</dd>
        </div>
      </dl>

      {job.lastResult && !failed ? (
        <div className="flex flex-col gap-1">
          <p className="app-meta">{t('result')}</p>
          <pre className="max-h-40 overflow-auto rounded-lg bg-muted px-3 py-2 font-mono text-xs">{prettyResult(job.lastResult)}</pre>
        </div>
      ) : null}

      {failed || job.error ? (
        <div className="flex flex-col gap-2 rounded-lg border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm">
          <p className="flex items-start gap-2 font-medium text-destructive">
            <AlertTriangleIcon className="mt-0.5 size-4 shrink-0" />
            <span className="break-words">{job.error ?? job.lastError}</span>
          </p>
          {job.lastErrorDetails ? (
            <details>
              <summary className="cursor-pointer text-xs text-muted-foreground">{t('stack')}</summary>
              <pre className="mt-2 max-h-72 overflow-auto font-mono text-xs whitespace-pre-wrap">{job.lastErrorDetails}</pre>
            </details>
          ) : null}
        </div>
      ) : null}
    </li>
  )
}
