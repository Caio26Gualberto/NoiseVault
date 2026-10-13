import { HeadphonesIcon, StoreIcon } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { AppCard } from '@/components/app-card'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppPageHeader } from '@/components/app-page-header'

type InstitutionalKey = 'podcasts' | 'store'

const icons: Record<InstitutionalKey, LucideIcon> = {
  podcasts: HeadphonesIcon,
  store: StoreIcon,
}

function InstitucionalPage({ page }: { page: InstitutionalKey }) {
  const { t } = useTranslation('institucional')
  const Icon = icons[page]
  const paragraphs = t(`${page}.body`, { returnObjects: true }) as string[]

  return (
    <div className="app-page-enter mx-auto flex max-w-2xl flex-col">
      <AppPageHeader title={t(`${page}.title`)} description={t(`${page}.description`)} />
      {page === 'podcasts' ? (
        <AppEmptyState icon={Icon} title={t('podcasts.soonTitle')} description={t('podcasts.soonDescription')} />
      ) : (
        <AppCard>
          <div className="flex flex-col gap-4 text-[15px] leading-relaxed">
            {paragraphs.map((paragraph) => (
              <p key={paragraph}>{paragraph}</p>
            ))}
          </div>
        </AppCard>
      )}
    </div>
  )
}

export function PodcastsPage() {
  return <InstitucionalPage page="podcasts" />
}

export function LojaParceiraPage() {
  return <InstitucionalPage page="store" />
}
