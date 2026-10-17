import { MailIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

type LegalSection = {
  title: string
  paragraphs: string[]
}

export const contactEmail = 'caiogualbertodev@outlook.com'

export function LegalDocument({ namespaceKey, children }: { namespaceKey: 'privacy' | 'dataDeletion'; children?: ReactNode }) {
  const { t } = useTranslation('institucional')
  const sections = t(`${namespaceKey}.sections`, { returnObjects: true, email: contactEmail }) as LegalSection[]

  return (
    <article className="flex flex-col gap-6">
      <header className="flex flex-col gap-1">
        <h1 className="app-h2">{t(`${namespaceKey}.title`)}</h1>
        <p className="app-meta">{t('legal.updated')}</p>
      </header>

      <p className="text-sm leading-relaxed">{t(`${namespaceKey}.intro`, { email: contactEmail })}</p>

      {sections.map((section) => (
        <section key={section.title} className="flex flex-col gap-2">
          <h2 className="app-h3">{section.title}</h2>
          {section.paragraphs.map((paragraph) => (
            <p key={paragraph} className="text-sm leading-relaxed text-muted-foreground">
              {paragraph}
            </p>
          ))}
        </section>
      ))}

      {children}

      <a
        href={`mailto:${contactEmail}`}
        className="flex items-center gap-2 self-start rounded-lg border px-3 py-2 text-sm font-medium text-primary hover:bg-accent"
      >
        <MailIcon className="size-4" aria-hidden="true" />
        {contactEmail}
      </a>
    </article>
  )
}
