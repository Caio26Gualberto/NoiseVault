import { BugIcon, ChevronRightIcon, LightbulbIcon } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import type { ReportKind } from '../report-description'

const kindIcon: Record<ReportKind, LucideIcon> = { bug: BugIcon, suggestion: LightbulbIcon }

const kinds: ReportKind[] = ['bug', 'suggestion']

export function ReportKindPicker({ onPick }: { onPick: (kind: ReportKind) => void }) {
  const { t } = useTranslation('suporte')

  return (
    <div className="mx-auto grid w-full max-w-3xl gap-4 sm:grid-cols-2">
      {kinds.map((kind) => {
        const Icon = kindIcon[kind]
        return (
          <button
            key={kind}
            type="button"
            onClick={() => onPick(kind)}
            className="group flex flex-col gap-4 rounded-xl border bg-card p-5 text-left transition-colors hover:border-primary/60 hover:bg-accent focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none md:p-6"
          >
            <span className="flex size-12 items-center justify-center rounded-full bg-primary/15 text-primary">
              <Icon className="size-6" />
            </span>
            <span className="flex flex-col gap-1">
              <span className="flex items-center gap-1 font-heading text-xl font-bold tracking-wide uppercase">
                {t(`kinds.${kind}.title`)}
                <ChevronRightIcon className="size-5 text-muted-foreground transition-transform group-hover:translate-x-0.5" />
              </span>
              <span className="text-sm text-muted-foreground">{t(`kinds.${kind}.description`)}</span>
            </span>
          </button>
        )
      })}
    </div>
  )
}
