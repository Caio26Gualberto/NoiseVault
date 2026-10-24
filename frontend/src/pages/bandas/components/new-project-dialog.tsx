import { MicVocalIcon, UsersRoundIcon } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { AppDialog } from '@/components/app-dialog'

import { BandKind, allBandKinds, bandCreatePath, bandKindKey } from '../band-kind'

const kindIcon: Record<BandKind, LucideIcon> = {
  [BandKind.band]: UsersRoundIcon,
  [BandKind.solo]: MicVocalIcon,
}

type NewProjectDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function NewProjectDialog({ open, onOpenChange }: NewProjectDialogProps) {
  const { t } = useTranslation('bandas')

  return (
    <AppDialog open={open} onOpenChange={onOpenChange} title={t('newProject.title')} description={t('newProject.description')}>
      <ul className="grid gap-3 sm:grid-cols-2">
        {allBandKinds.map((kind) => {
          const Icon = kindIcon[kind]
          return (
            <li key={kind}>
              <Link
                to={bandCreatePath(kind)}
                onClick={() => onOpenChange(false)}
                className="flex h-full flex-col gap-2 rounded-xl border bg-card p-4 transition-colors hover:bg-accent focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
              >
                <Icon className="size-6 text-primary" />
                <span className="font-heading text-lg font-semibold tracking-wide uppercase">{t(`kinds.${bandKindKey[kind]}.title`)}</span>
                <span className="text-sm text-muted-foreground">{t(`kinds.${bandKindKey[kind]}.description`)}</span>
              </Link>
            </li>
          )
        })}
      </ul>
    </AppDialog>
  )
}
