import { CompassIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { homePath } from '@/auth/routes'
import { AppButton } from '@/components/app-button'
import { AppEmptyState } from '@/components/app-empty-state'

export function NaoEncontradoPage() {
  const { t } = useTranslation()

  return (
    <div className="app-page-enter mx-auto flex max-w-2xl flex-col py-10">
      <AppEmptyState
        icon={CompassIcon}
        title={t('notFound.title')}
        description={t('notFound.description')}
        action={
          <AppButton asChild>
            <Link to={homePath}>{t('notFound.action')}</Link>
          </AppButton>
        }
      />
    </div>
  )
}
