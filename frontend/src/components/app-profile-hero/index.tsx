import type { ReactNode } from 'react'

import { AppAvatar } from '@/components/app-avatar'
import { cn } from '@/lib/utils'

type Stat = { label: string; value: number | string }

type AppProfileHeroProps = {
  name: string
  handle: string
  avatarUrl?: string | null
  bannerUrl?: string | null
  bannerPosition?: number
  verified?: boolean
  subtitle?: ReactNode
  stats?: Stat[]
  actions?: ReactNode
  children?: ReactNode
  className?: string
}

export function AppProfileHero({
  name,
  handle,
  avatarUrl,
  bannerUrl,
  bannerPosition = 50,
  verified = false,
  subtitle,
  stats = [],
  actions,
  children,
  className,
}: AppProfileHeroProps) {
  return (
    <section className={cn('overflow-hidden rounded-xl border bg-card', className)}>
      <div className="relative aspect-[3/1] w-full bg-muted md:aspect-[4/1]">
        {bannerUrl ? (
          <img
            src={bannerUrl}
            alt=""
            className="size-full object-cover"
            style={{ objectPosition: `center ${bannerPosition}%` }}
          />
        ) : (
          <div className="size-full bg-[radial-gradient(ellipse_at_bottom_left,_var(--primary)_0%,_transparent_60%)] opacity-40" />
        )}
        <div className="absolute inset-x-0 bottom-0 h-1/2 bg-gradient-to-t from-card to-transparent" />
      </div>

      <div className="flex flex-col gap-4 px-4 pb-4 md:px-6 md:pb-6">
        <div className="-mt-12 flex flex-col gap-3 md:-mt-16 md:flex-row md:items-end md:justify-between">
          <div className="flex items-end gap-4">
            <AppAvatar name={name} src={avatarUrl} size="2xl" verified={verified} className="ring-4 ring-card rounded-full" />
            <div className="flex min-w-0 flex-col pb-1">
              <h1 className="app-h2 truncate">{name}</h1>
              <p className="app-meta truncate">{handle}</p>
            </div>
          </div>
          {actions ? <div className="flex flex-wrap gap-2 md:pb-1">{actions}</div> : null}
        </div>

        {subtitle ? <div className="text-sm text-muted-foreground">{subtitle}</div> : null}
        {children}

        {stats.length > 0 ? (
          <dl className="flex flex-wrap gap-x-6 gap-y-2 border-t pt-4">
            {stats.map((stat) => (
              <div key={stat.label} className="flex items-baseline gap-1.5">
                <dd className="font-heading text-xl font-semibold tabular-nums">{stat.value}</dd>
                <dt className="app-meta">{stat.label}</dt>
              </div>
            ))}
          </dl>
        ) : null}
      </div>
    </section>
  )
}
