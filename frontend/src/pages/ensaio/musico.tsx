import { CheckIcon, ExternalLinkIcon, MinusIcon, PencilIcon, UserRoundSearchIcon } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'

import { ApiCodes } from '@/api/api-codes'
import { feedback } from '@/api/feedback-sink'
import { ApiError } from '@/api/types'
import { AppButton } from '@/components/app-button'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppSkeleton } from '@/components/app-skeleton'
import { ensaioPath, isExternalUrl, mainUrl } from '@/ensaio/frame'
import { cn } from '@/lib/utils'
import { formatCity } from '@/pages/perfil/city'
import { useMusicGenreLabel } from '@/pages/perfil/genres'

import { InviteDialog } from './components/invite-dialog'
import { PaperSection, PaperSheet } from './components/paper-sheet'
import {
  allDayPeriods,
  allWeekDays,
  commitmentKey,
  dayPeriodKey,
  goalKey,
  levelKey,
  useDistanceLabel,
  useInstrumentLabel,
  useReachLabel,
  weekDayKey,
} from './ensaio-status'
import { useOwnedBands } from './hooks/use-owned-bands'
import { getMusician } from './services/ensaio-service'
import type { MusicianProfileDto } from './services/ensaio-service'

export function EnsaioMusicoPage() {
  const { t } = useTranslation('ensaio')
  const { nickname = '' } = useParams()
  const [profile, setProfile] = useState<MusicianProfileDto | null>(null)
  const [state, setState] = useState<'loading' | 'ready' | 'missing'>('loading')
  const [inviting, setInviting] = useState(false)
  const { bands } = useOwnedBands()
  const instrumentLabel = useInstrumentLabel()
  const genreLabel = useMusicGenreLabel()
  const reachLabel = useReachLabel()
  const distanceLabel = useDistanceLabel()

  useEffect(() => {
    let cancelled = false
    setState('loading')
    getMusician(nickname)
      .then((data) => {
        if (cancelled) return
        setProfile(data)
        setState('ready')
      })
      .catch((error: unknown) => {
        if (cancelled) return
        if (error instanceof ApiError && error.code !== ApiCodes.musicianProfileNotFound) feedback.error(error.code, error.backendMessage)
        setState('missing')
      })
    return () => {
      cancelled = true
    }
  }, [nickname])

  if (state === 'loading') return <AppSkeleton preset="lines" count={8} className="mx-auto max-w-3xl" />

  if (state === 'missing' || !profile) {
    return <AppEmptyState icon={UserRoundSearchIcon} title={t('profile.notFound')} />
  }

  const main = profile.instruments[0]
  const photo = profile.photoUrl ?? profile.person.avatarUrl
  const profileHref = mainUrl(`/perfil/${profile.person.nickname}`)
  const slots = new Set(profile.availability.map((slot) => `${slot.day}-${slot.period}`))
  const structure = [
    profile.hasGear ? t('profile.hasGear') : null,
    profile.hasTransport ? t('profile.hasTransport') : null,
    profile.hasRehearsalSpace ? t('profile.hasRehearsalSpace') : null,
  ].filter((item): item is string => item !== null)

  return (
    <div className="app-page-enter mx-auto flex max-w-3xl flex-col gap-6">
      <PaperSheet className="flex flex-col gap-6">
        <div className="flex flex-col gap-5 sm:flex-row sm:items-center">
          <span className="ensaio-halftone h-40 w-full shrink-0 overflow-hidden sm:h-36 sm:w-48">
            {photo ? <img src={photo} alt="" className="size-full object-cover mix-blend-multiply grayscale contrast-125" /> : null}
          </span>
          <div className="flex min-w-0 flex-col gap-1">
            <span className={cn('self-start font-typewriter text-sm', profile.isLookingForBand ? 'text-primary' : 'text-paper-muted')}>
              {t(profile.isLookingForBand ? 'profile.lookingBadge' : 'profile.notLookingBadge')}
            </span>
            {main ? <span className="font-heading text-4xl leading-none font-bold uppercase md:text-5xl">{instrumentLabel(main.instrument, main.otherName)}</span> : null}
            <span className="text-xl font-semibold">{profile.person.name}</span>
            <span className="text-paper-muted">
              @{profile.person.nickname}
              {formatCity(profile) ? ` · ${formatCity(profile)}` : ''}
              {profile.distanceKm !== null ? ` · ${distanceLabel(profile.distanceKm)}` : ''}
            </span>
          </div>
        </div>

        <PaperSection title={t('profile.instruments')}>
          <ul className="flex flex-wrap gap-2">
            {profile.instruments.map((item, index) => (
              <li key={index} className="border border-paper-rule px-3 py-1.5">
                <span className="font-semibold">{instrumentLabel(item.instrument, item.otherName)}</span>
                <span className="text-paper-muted"> · {t(`levels.${levelKey[item.level]}`)}</span>
              </li>
            ))}
          </ul>
        </PaperSection>

        <PaperSection title={t('profile.plays')}>
          <ul className="flex flex-col gap-1">
            <li>{t(`goals.${goalKey[profile.goal]}`)}</li>
            <li>{t(`commitments.${commitmentKey[profile.commitment]}`)}</li>
            <li>{reachLabel(profile.reachKm)}</li>
          </ul>
          {profile.genres.length > 0 ? (
            <p className="text-paper-muted">
              <span className="font-semibold text-paper-foreground">{t('profile.genres')}: </span>
              {profile.genres.map(genreLabel).join(', ')}
            </p>
          ) : null}
        </PaperSection>

        <PaperSection title={t('profile.availability')}>
          {profile.availability.length === 0 ? (
            <p className="text-paper-muted">{t('profile.noAvailability')}</p>
          ) : (
            <div className="relative overflow-x-auto">
              <table className="w-full min-w-[420px] border-collapse text-center text-sm">
                <thead>
                  <tr>
                    <th scope="col" className="w-20" />
                    {allWeekDays.map((day) => (
                      <th key={day} scope="col" className="pb-2 font-heading font-medium tracking-wide uppercase">
                        {t(`weekDays.${weekDayKey[day]}`)}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {allDayPeriods.map((period) => (
                    <tr key={period} className="border-t border-dashed border-paper-rule">
                      <th scope="row" className="py-2 text-left font-medium">
                        {t(`periods.${dayPeriodKey[period]}`)}
                      </th>
                      {allWeekDays.map((day) => {
                        const free = slots.has(`${day}-${period}`)
                        return (
                          <td key={day} className="py-2">
                            {free ? <CheckIcon className="mx-auto size-4 text-primary" aria-label={t('profile.availability')} /> : <MinusIcon className="mx-auto size-4 text-paper-rule" aria-hidden="true" />}
                          </td>
                        )
                      })}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </PaperSection>

        <PaperSection title={t('profile.structure')}>
          {structure.length === 0 ? (
            <p className="text-paper-muted">{t('profile.noStructure')}</p>
          ) : (
            <ul className="flex flex-wrap gap-x-5 gap-y-1">
              {structure.map((item) => (
                <li key={item} className="flex items-center gap-1.5">
                  <CheckIcon className="size-4 text-primary" aria-hidden="true" />
                  {item}
                </li>
              ))}
            </ul>
          )}
        </PaperSection>

        {profile.sampleLinks.length > 0 || profile.influences ? (
          <PaperSection title={t('profile.sound')}>
            {profile.sampleLinks.length > 0 ? (
              <ul className="flex flex-col gap-1.5">
                {profile.sampleLinks.map((link) => (
                  <li key={link}>
                    <a href={link} target="_blank" rel="noopener noreferrer" className="inline-flex items-center gap-1.5 break-all text-primary hover:underline">
                      <ExternalLinkIcon className="size-4 shrink-0" aria-hidden="true" />
                      {link.replace(/^https:\/\/(www\.)?/, '')}
                    </a>
                  </li>
                ))}
              </ul>
            ) : null}
            {profile.influences ? (
              <p className="whitespace-pre-line">
                <span className="font-semibold">{t('profile.influences')}: </span>
                {profile.influences}
              </p>
            ) : null}
          </PaperSection>
        ) : null}
      </PaperSheet>

      <div className="flex flex-wrap gap-2">
        {profile.isMine ? (
          <AppButton asChild>
            <Link to={ensaioPath('/minha-ficha')}>
              <PencilIcon />
              {t('profile.editMine')}
            </Link>
          </AppButton>
        ) : bands.length > 0 ? (
          <AppButton onClick={() => setInviting(true)}>{t('musicians.callToBand')}</AppButton>
        ) : null}
        <AppButton asChild intent="secondary">
          {isExternalUrl(profileHref) ? <a href={profileHref}>{t('profile.seeProfile')}</a> : <Link to={profileHref}>{t('profile.seeProfile')}</Link>}
        </AppButton>
      </div>

      <InviteDialog
        open={inviting}
        onOpenChange={setInviting}
        bands={bands}
        person={profile.person}
        instrument={main ? instrumentLabel(main.instrument, main.otherName) : ''}
      />
    </div>
  )
}
