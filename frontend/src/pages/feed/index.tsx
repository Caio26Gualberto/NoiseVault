import { NewspaperIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { AppEmptyState } from '@/components/app-empty-state'
import { AppPageHeader } from '@/components/app-page-header'

export function FeedPage() {
  const { t } = useTranslation('feed')

  return (
    <div className="app-page-enter mx-auto flex max-w-2xl flex-col">
      <AppPageHeader title={t('title')} description={t('description')} />
      <AppEmptyState icon={NewspaperIcon} title={t('soonTitle')} description={t('soonDescription')} />
    </div>
  )
}
