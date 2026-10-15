import type { LucideIcon } from 'lucide-react'
import {
  DiscIcon,
  ExternalLinkIcon,
  GemIcon,
  GiftIcon,
  GuitarIcon,
  MailIcon,
  MapPinIcon,
  MessageCircleIcon,
  ShirtIcon,
  TicketIcon,
  TruckIcon,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { AppBadge } from '@/components/app-badge'
import { AppButton } from '@/components/app-button'

const storeUrl = 'https://www.metalmusic.com.br/'
const whatsappUrl = 'https://wa.me/5511934587444'
const email = 'lojametalcds@hotmail.com'
const mapsUrl = 'https://www.google.com/maps/search/?api=1&query=Rua+Alvares+de+Azevedo+159+Centro+Santo+Andr%C3%A9+SP'

type Category = { key: string; icon: LucideIcon; href: string }

const categories: Category[] = [
  { key: 'apparel', icon: ShirtIcon, href: `${storeUrl}roupas-e-calcados` },
  { key: 'records', icon: DiscIcon, href: `${storeUrl}cds-dvds-e-lps` },
  { key: 'instruments', icon: GuitarIcon, href: `${storeUrl}instrumentos-musicais` },
  { key: 'accessories', icon: GemIcon, href: `${storeUrl}acessorios` },
  { key: 'tickets', icon: TicketIcon, href: `${storeUrl}ingressos` },
  { key: 'giftCard', icon: GiftIcon, href: `${storeUrl}gift-card` },
]

export function LojaParceiraPage() {
  const { t } = useTranslation('institucional')

  return (
    <div className="app-page-enter mx-auto flex max-w-5xl flex-col gap-6">
      <section className="relative overflow-hidden rounded-xl border bg-card">
        <div aria-hidden className="absolute inset-0 bg-[radial-gradient(ellipse_at_top_left,_var(--primary)_0%,_transparent_60%)] opacity-25" />
        <div aria-hidden className="absolute inset-y-0 right-0 hidden w-1/3 bg-[repeating-linear-gradient(135deg,_var(--primary)_0,_var(--primary)_2px,_transparent_2px,_transparent_14px)] opacity-10 md:block" />
        <div className="relative flex flex-col gap-6 p-6 md:p-10">
          <div className="flex flex-wrap items-center gap-2">
            <AppBadge tone="brand">{t('store.badge')}</AppBadge>
            <span className="app-meta">{t('store.location')}</span>
          </div>
          <div className="flex flex-col gap-2">
            <h1 className="font-heading text-4xl font-bold tracking-wide uppercase md:text-6xl">{t('store.name')}</h1>
            <p className="max-w-2xl text-base text-muted-foreground md:text-lg">{t('store.lead')}</p>
          </div>
          <div className="flex flex-col gap-3 sm:flex-row">
            <AppButton asChild size="lg">
              <a href={storeUrl} target="_blank" rel="noopener noreferrer">
                {t('store.visit')}
                <ExternalLinkIcon className="size-4" />
              </a>
            </AppButton>
            <AppButton asChild intent="secondary" size="lg">
              <a href={whatsappUrl} target="_blank" rel="noopener noreferrer">
                <MessageCircleIcon className="size-4" />
                {t('store.whatsapp')}
              </a>
            </AppButton>
          </div>
        </div>
      </section>

      <section className="flex flex-col gap-4">
        <header className="flex flex-col gap-1">
          <h2 className="app-h2">{t('store.categoriesTitle')}</h2>
          <p className="text-sm text-muted-foreground">{t('store.categoriesDescription')}</p>
        </header>
        <ul className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {categories.map((category) => {
            const Icon = category.icon
            return (
              <li key={category.key}>
                <a
                  href={category.href}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="group flex h-full flex-col gap-3 rounded-xl border bg-card p-5 transition-colors hover:border-primary/60 hover:bg-accent focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
                >
                  <span className="flex size-11 items-center justify-center rounded-lg bg-primary/15 text-primary">
                    <Icon className="size-5" />
                  </span>
                  <span className="flex flex-col gap-1">
                    <span className="font-heading text-lg font-semibold tracking-wide uppercase">{t(`store.categories.${category.key}.title`)}</span>
                    <span className="text-sm text-muted-foreground">{t(`store.categories.${category.key}.description`)}</span>
                  </span>
                </a>
              </li>
            )
          })}
        </ul>
      </section>

      <section className="grid gap-4 md:grid-cols-[minmax(0,3fr)_minmax(0,2fr)]">
        <div className="flex flex-col gap-4 rounded-xl border bg-card p-6">
          <h2 className="app-h2">{t('store.visitTitle')}</h2>
          <ul className="flex flex-col gap-3 text-sm">
            <li className="flex items-start gap-3">
              <MapPinIcon className="mt-0.5 size-4 shrink-0 text-primary" />
              <span className="flex flex-col">
                <span className="font-medium">{t('store.address')}</span>
                <a href={mapsUrl} target="_blank" rel="noopener noreferrer" className="text-primary hover:underline">
                  {t('store.openMap')}
                </a>
              </span>
            </li>
            <li className="flex items-start gap-3">
              <MessageCircleIcon className="mt-0.5 size-4 shrink-0 text-primary" />
              <a href={whatsappUrl} target="_blank" rel="noopener noreferrer" className="hover:underline">
                {t('store.phone')}
              </a>
            </li>
            <li className="flex items-start gap-3">
              <MailIcon className="mt-0.5 size-4 shrink-0 text-primary" />
              <a href={`mailto:${email}`} className="hover:underline">
                {email}
              </a>
            </li>
            <li className="flex items-start gap-3">
              <TruckIcon className="mt-0.5 size-4 shrink-0 text-primary" />
              <span>{t('store.shipping')}</span>
            </li>
          </ul>
        </div>
        <div className="flex flex-col justify-between gap-4 rounded-xl border border-primary/40 bg-primary/10 p-6">
          <div className="flex flex-col gap-2">
            <h2 className="app-h3">{t('store.communityTitle')}</h2>
            <p className="text-sm leading-relaxed text-muted-foreground">{t('store.communityBody')}</p>
          </div>
          <AppButton asChild intent="secondary">
            <a href={storeUrl} target="_blank" rel="noopener noreferrer">
              {t('store.visit')}
              <ExternalLinkIcon className="size-4" />
            </a>
          </AppButton>
        </div>
      </section>
    </div>
  )
}
