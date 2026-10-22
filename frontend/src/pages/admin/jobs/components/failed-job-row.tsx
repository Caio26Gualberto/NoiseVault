import { useTranslation } from 'react-i18next'

import { formatDateTime, formatRelative } from '@/lib/format'

import type { FailedJobDto } from '../services/jobs-admin-service'

type FailedJobRowProps = {
  job: FailedJobDto
}

export function FailedJobRow({ job }: FailedJobRowProps) {
  const { t } = useTranslation('jobs')

  return (
    <li className="flex flex-col gap-2 p-4">
      <div className="flex flex-col gap-1 sm:flex-row sm:items-baseline sm:justify-between sm:gap-4">
        <p className="font-medium">{job.name ?? t('unnamed')}</p>
        {job.failedAt ? (
          <span className="app-meta shrink-0" title={formatDateTime(job.failedAt)}>
            {formatRelative(job.failedAt)}
          </span>
        ) : null}
      </div>
      <p className="app-meta flex flex-wrap gap-x-2 gap-y-1">
        <code className="font-mono text-xs">#{job.jobId}</code>
        {job.recurringJobId ? <code className="font-mono text-xs">· {job.recurringJobId}</code> : null}
        {job.queue ? <span>· {t('queue', { queue: job.queue })}</span> : null}
      </p>
      <p className="text-sm break-words text-destructive">
        {job.exceptionType ? <code className="font-mono text-xs">{job.exceptionType}: </code> : null}
        {job.exceptionMessage}
      </p>
      {job.exceptionDetails ? (
        <details>
          <summary className="cursor-pointer text-xs text-muted-foreground">{t('stack')}</summary>
          <pre className="mt-2 max-h-72 overflow-auto rounded-lg bg-muted px-3 py-2 font-mono text-xs whitespace-pre-wrap">{job.exceptionDetails}</pre>
        </details>
      ) : null}
    </li>
  )
}
