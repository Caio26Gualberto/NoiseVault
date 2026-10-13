import { UserRoundIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { AppEmptyState } from '@/components/app-empty-state'
import { AppPageHeader } from '@/components/app-page-header'

export function PerfilPage() {
  const { t } = useTranslation()

  return (
    <div className="app-page-enter flex flex-col">
      <AppPageHeader title={t('layout:nav.profile')} />
      <AppEmptyState icon={UserRoundIcon} title={t('soon.title')} description={t('soon.description')} />
    </div>
  )
}
