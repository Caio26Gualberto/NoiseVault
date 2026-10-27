import { CircleCheckIcon, InboxIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router-dom'

import { homePath } from '@/auth/routes'
import { AppButton } from '@/components/app-button'
import { AppCard } from '@/components/app-card'
import { useAppConfirm } from '@/components/app-confirm'
import { AppPageHeader } from '@/components/app-page-header'

import { ReportKindPicker } from './components/report-kind-picker'
import { BugReport, SuggestionReport } from './components/report-steps'
import { useReportSubmit } from './hooks/use-report-submit'
import { reportKindFromParam, reportKindParam } from './report-description'
import type { ReportKind } from './report-description'
import { myTicketPath, myTicketsPath } from './support-paths'

const kindParam = 'tipo'

export function AjudaPage() {
  const { t } = useTranslation('suporte')
  const confirm = useAppConfirm()
  const [params, setParams] = useSearchParams()
  const kind = reportKindFromParam(params.get(kindParam))
  const { origin, submitting, created, submit } = useReportSubmit()

  const pick = (next: ReportKind) => setParams({ [kindParam]: reportKindParam[next] })
  const leave = () => setParams({}, { replace: true })

  const exit = (dirty: boolean) => {
    if (!dirty) return leave()
    confirm({
      title: t('wizard.exitTitle'),
      message: t('wizard.exitMessage'),
      confirmLabel: t('wizard.exitConfirm'),
      danger: true,
      onConfirm: leave,
    })
  }

  const flowProps = { origin, submitting, onSubmit: submit, onExit: exit }

  return (
    <div className="app-page-enter flex flex-col gap-6">
      <AppPageHeader
        title={t('title')}
        description={t('description')}
        actions={
          <AppButton intent="neutral" asChild>
            <Link to={myTicketsPath}>
              <InboxIcon />
              {t('myTickets')}
            </Link>
          </AppButton>
        }
      />

      {created ? (
        <AppCard className="mx-auto w-full max-w-2xl">
          <div className="flex flex-col items-center gap-4 py-4 text-center">
            <div className="flex size-14 items-center justify-center rounded-full bg-success/15 text-success">
              <CircleCheckIcon className="size-7" />
            </div>
            <div className="flex flex-col gap-1">
              <h2 className="app-h3">{t(kind === 'suggestion' ? 'sent.suggestionTitle' : 'sent.bugTitle')}</h2>
              <p className="text-sm text-muted-foreground">{t('sent.description', { protocol: created.protocol })}</p>
            </div>
            <div className="flex flex-col gap-2 sm:flex-row">
              <AppButton asChild>
                <Link to={myTicketPath(created.id)}>{t('sent.follow')}</Link>
              </AppButton>
              <AppButton intent="neutral" asChild>
                <Link to={homePath}>{t('sent.back')}</Link>
              </AppButton>
            </div>
          </div>
        </AppCard>
      ) : kind === 'bug' ? (
        <BugReport {...flowProps} />
      ) : kind === 'suggestion' ? (
        <SuggestionReport {...flowProps} />
      ) : (
        <ReportKindPicker onPick={pick} />
      )}
    </div>
  )
}
