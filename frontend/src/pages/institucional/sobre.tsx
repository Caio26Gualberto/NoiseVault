import type { LucideIcon } from 'lucide-react'
import { BotIcon, BriefcaseIcon, CameraIcon, CodeIcon, ExternalLinkIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import caioPhoto from '@/assets/caio.jpg'
import { AppAvatar } from '@/components/app-avatar'

type SocialLink = { labelKey: string; href: string; icon: LucideIcon }

const socialLinks: SocialLink[] = [
  { labelKey: 'about.links.instagram', href: 'https://www.instagram.com/caio_gualbertoo/', icon: CameraIcon },
  { labelKey: 'about.links.github', href: 'https://github.com/Caio26Gualberto', icon: CodeIcon },
  { labelKey: 'about.links.linkedin', href: 'https://www.linkedin.com/in/caio-faria-gualberto-50706019a/', icon: BriefcaseIcon },
]

export function SobrePage() {
  const { t } = useTranslation('institucional')
  const paragraphs = t('about.body', { returnObjects: true }) as string[]

  return (
    <div className="app-page-enter mx-auto flex max-w-5xl flex-col">
      <section className="grid overflow-hidden rounded-xl border bg-card md:grid-cols-[minmax(0,1fr)_minmax(0,2fr)]">
        <aside className="relative flex flex-col items-center gap-4 bg-primary/15 p-6 text-center md:p-8">
          <div aria-hidden className="absolute inset-0 bg-[radial-gradient(ellipse_at_top,_var(--primary)_0%,_transparent_70%)] opacity-30" />
          <div className="relative flex flex-col items-center gap-4">
            <AppAvatar name={t('about.name')} src={caioPhoto} size="2xl" className="rounded-full ring-4 ring-card" />
            <div className="flex flex-col gap-1">
              <h2 className="app-h2">{t('about.name')}</h2>
              <p className="text-sm text-muted-foreground">{t('about.role')}</p>
            </div>
            <ul className="flex w-full max-w-xs flex-col gap-1">
              {socialLinks.map((link) => {
                const Icon = link.icon
                return (
                  <li key={link.href}>
                    <a
                      href={link.href}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors hover:bg-accent focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
                    >
                      <Icon className="size-4 text-primary" />
                      <span className="flex-1 text-left">{t(link.labelKey)}</span>
                      <ExternalLinkIcon className="size-3.5 text-muted-foreground" />
                    </a>
                  </li>
                )
              })}
            </ul>
          </div>
        </aside>

        <div className="flex flex-col gap-6 p-6 md:p-10">
          <header className="flex flex-col gap-3">
            <h1 className="app-h1">{t('about.title')}</h1>
            <span aria-hidden className="h-1 w-14 rounded-full bg-primary" />
          </header>

          <div className="flex flex-col gap-4 text-[15px] leading-relaxed md:text-base md:leading-[1.8]">
            {paragraphs.map((paragraph) => (
              <p key={paragraph}>{paragraph}</p>
            ))}
          </div>

          <p className="flex items-center gap-2 rounded-lg border-l-4 border-primary bg-primary/5 px-4 py-3 text-[13px] text-muted-foreground italic">
            <BotIcon className="size-4 shrink-0" />
            {t('about.note')}
          </p>
        </div>
      </section>
    </div>
  )
}
