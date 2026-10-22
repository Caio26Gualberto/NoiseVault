import { useCallback, useEffect, useState } from 'react'

import { emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { defaultPageSize } from '@/components/app-pagination'
import { usePolling } from '@/hooks/use-polling'

import { isRunning } from '../job-state'
import { getFailedJobs, getRecurringJobs, triggerRecurringJob } from '../services/jobs-admin-service'
import type { FailedJobDto, RecurringJobDto } from '../services/jobs-admin-service'

export type JobsTab = 'recurring' | 'failures'

const runningPollMs = 3000

export function useJobsAdminPage() {
  const [tab, setTab] = useState<JobsTab>('recurring')
  const [jobs, setJobs] = useState<RecurringJobDto[]>([])
  const [jobsLoading, setJobsLoading] = useState(true)
  const [triggering, setTriggering] = useState<string | null>(null)
  const [failures, setFailures] = useState<PagedResult<FailedJobDto>>(() => emptyPage(defaultPageSize))
  const [failuresPage, setFailuresPage] = useState(1)
  const [failuresLoading, setFailuresLoading] = useState(true)

  const loadJobs = useCallback(async (quiet: boolean) => {
    const data = await getRecurringJobs(quiet).catch(() => null)
    if (data) setJobs(data)
    setJobsLoading(false)
  }, [])

  useEffect(() => {
    let cancelled = false
    getRecurringJobs()
      .then((data) => {
        if (!cancelled) setJobs(data)
      })
      .catch(() => {
        if (!cancelled) setJobs([])
      })
      .finally(() => {
        if (!cancelled) setJobsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    if (tab !== 'failures') return
    let cancelled = false
    getFailedJobs({ page: failuresPage, pageSize: defaultPageSize })
      .then((data) => {
        if (!cancelled) setFailures(data)
      })
      .catch(() => {
        if (!cancelled) setFailures(emptyPage(defaultPageSize))
      })
      .finally(() => {
        if (!cancelled) setFailuresLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [tab, failuresPage])

  const anyRunning = jobs.some((job) => isRunning(job.lastJobState))

  usePolling(() => loadJobs(true), anyRunning ? runningPollMs : 0)

  const changeTab = (value: JobsTab) => {
    if (value === 'failures') {
      setFailuresLoading(true)
      setFailuresPage(1)
    }
    setTab(value)
  }

  const changeFailuresPage = (page: number) => {
    setFailuresLoading(true)
    setFailuresPage(page)
  }

  const trigger = async (job: RecurringJobDto) => {
    setTriggering(job.id)
    try {
      await triggerRecurringJob(job.id)
    } catch {
      return
    } finally {
      setTriggering(null)
      await loadJobs(true)
    }
  }

  return {
    tab,
    changeTab,
    jobs,
    jobsLoading,
    triggering,
    trigger,
    refresh: () => loadJobs(false),
    failures,
    failuresLoading,
    changeFailuresPage,
  }
}
