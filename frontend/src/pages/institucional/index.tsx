import { HeadphonesIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { AppEmptyState } from '@/components/app-empty-state'
import { AppPageHeader } from '@/components/app-page-header'

export function PodcastsPage() {
  const { t } = useTranslation('institucional')

  return (
    <div className="app-page-enter mx-auto flex max-w-2xl flex-col">
      <AppPageHeader title={t('podcasts.title')} description={t('podcasts.description')} />
      <AppEmptyState icon={HeadphonesIcon} title={t('podcasts.soonTitle')} description={t('podcasts.soonDescription')} />
    </div>
  )
}
