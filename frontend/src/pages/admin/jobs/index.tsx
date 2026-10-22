import { ActivityIcon, CircleCheckIcon, RefreshCwIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { Permissions } from '@/auth/permissions'
import { useCan } from '@/auth/use-can'
import { AppButton } from '@/components/app-button'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppPageHeader } from '@/components/app-page-header'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { useDocumentTitle } from '@/hooks/use-document-title'

import { FailedJobRow } from './components/failed-job-row'
import { RecurringJobRow } from './components/recurring-job-row'
import { useJobsAdminPage } from './hooks/use-jobs-admin-page'
import type { JobsTab } from './hooks/use-jobs-admin-page'

export function JobsAdminPage() {
  const { t } = useTranslation('jobs')
  const can = useCan()
  const page = useJobsAdminPage()
  const canManage = can(Permissions.systemJobsManage)
  useDocumentTitle(t('title'))

  return (
    <div className="app-page-enter flex flex-col">
      <AppPageHeader
        title={t('title')}
        description={t('description')}
        actions={
          page.tab === 'recurring' ? (
            <AppButton intent="neutral" onClick={() => void page.refresh()}>
              <RefreshCwIcon />
              {t('refresh')}
            </AppButton>
          ) : null
        }
      />

      <Tabs value={page.tab} onValueChange={(value) => page.changeTab(value as JobsTab)} className="mb-4">
        <TabsList>
          <TabsTrigger value="recurring">{t('tabs.recurring')}</TabsTrigger>
          <TabsTrigger value="failures">{t('tabs.failures')}</TabsTrigger>
        </TabsList>
      </Tabs>

      {page.tab === 'recurring' ? (
        page.jobsLoading ? (
          <AppSkeleton preset="list" count={5} />
        ) : page.jobs.length === 0 ? (
          <AppEmptyState icon={ActivityIcon} title={t('empty.recurring')} />
        ) : (
          <ul className="divide-y rounded-xl border">
            {page.jobs.map((job) => (
              <RecurringJobRow
                key={job.id}
                job={job}
                canManage={canManage}
                triggering={page.triggering === job.id}
                onTrigger={(target) => void page.trigger(target)}
              />
            ))}
          </ul>
        )
      ) : page.failuresLoading ? (
        <AppSkeleton preset="list" count={5} />
      ) : page.failures.items.length === 0 ? (
        <AppEmptyState icon={CircleCheckIcon} title={t('empty.failures')} description={t('empty.failuresDescription')} />
      ) : (
        <>
          <ul className="divide-y rounded-xl border">
            {page.failures.items.map((job) => (
              <FailedJobRow key={job.jobId} job={job} />
            ))}
          </ul>
          <AppPagination
            page={page.failures.page}
            totalPages={page.failures.totalPages}
            totalCount={page.failures.totalCount}
            onChange={page.changeFailuresPage}
            loading={page.failuresLoading}
          />
        </>
      )}
    </div>
  )
}
