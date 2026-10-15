import { CircleSlashIcon, EllipsisIcon, MegaphoneIcon, PencilIcon, RefreshCwIcon, ScissorsIcon, Trash2Icon, UserCheckIcon } from 'lucide-react'
import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router-dom'

import { emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { AppAvatar } from '@/components/app-avatar'
import { AppBadge } from '@/components/app-badge'
import type { AppBadgeTone } from '@/components/app-badge'
import { AppButton, AppIconButton } from '@/components/app-button'
import { useAppConfirm } from '@/components/app-confirm'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu'
import { ensaioPath, isExternalUrl, mainUrl } from '@/ensaio/frame'
import { formatCity } from '@/pages/perfil/city'

import { InterestDialog } from './components/interest-dialog'
import { PaperSheet } from './components/paper-sheet'
import { InterestStatus, OpeningStatus, goalKey, interestStatusKey, openingStatusKey, useInstrumentLabel } from './ensaio-status'
import {
  closeOpening,
  deleteOpening,
  dismissInterested,
  getOpening,
  getOpeningInterests,
  inviteInterested,
  renewOpening,
  withdrawInterest,
} from './services/ensaio-service'
import type { BandOpeningDto, OpeningInterestDto } from './services/ensaio-service'

const interestsPageSize = 10

const interestTone: Record<InterestStatus, AppBadgeTone> = {
  [InterestStatus.pending]: 'warning',
  [InterestStatus.invited]: 'success',
  [InterestStatus.dismissed]: 'neutral',
}

function statusOf(opening: BandOpeningDto) {
  if (opening.status === OpeningStatus.open && opening.isExpired) return 'expired'
  return openingStatusKey[opening.status]
}

export function EnsaioVagaPage() {
  const { t } = useTranslation('ensaio')
  const { id } = useParams()
  const openingId = Number(id)
  const navigate = useNavigate()
  const confirm = useAppConfirm()
  const instrumentLabel = useInstrumentLabel()
  const [opening, setOpening] = useState<BandOpeningDto | null>(null)
  const [state, setState] = useState<'loading' | 'ready' | 'missing'>('loading')
  const [interestOpen, setInterestOpen] = useState(false)

  useEffect(() => {
    let cancelled = false
    setState('loading')
    getOpening(openingId)
      .then((data) => {
        if (cancelled) return
        setOpening(data)
        setState('ready')
      })
      .catch(() => {
        if (!cancelled) setState('missing')
      })
    return () => {
      cancelled = true
    }
  }, [openingId])

  if (state === 'loading') return <AppSkeleton preset="lines" count={8} className="mx-auto max-w-3xl" />

  if (state === 'missing' || !opening) {
    return (
      <AppEmptyState
        icon={MegaphoneIcon}
        title={t('opening.notFound')}
        action={
          <AppButton asChild intent="secondary">
            <Link to={ensaioPath('/vagas')}>{t('opening.back')}</Link>
          </AppButton>
        }
      />
    )
  }

  const status = statusOf(opening)
  const isOpen = status === 'open'
  const label = instrumentLabel(opening.instrument, opening.otherName)
  const bandHref = mainUrl(`/bandas/${opening.band.slug}`)

  const run = async (action: () => Promise<BandOpeningDto>) => {
    try {
      setOpening(await action())
    } catch {
      return
    }
  }

  const askClose = (filled: boolean) =>
    confirm({
      title: t(filled ? 'opening.filledTitle' : 'opening.closeTitle'),
      message: t(filled ? 'opening.filledDescription' : 'opening.closeDescription'),
      confirmLabel: t(filled ? 'opening.markFilled' : 'opening.close'),
      onConfirm: () => run(() => closeOpening(opening.id, filled)),
    })

  const askDelete = () =>
    confirm({
      title: t('opening.deleteTitle'),
      message: t('opening.deleteDescription'),
      confirmLabel: t('opening.delete'),
      danger: true,
      onConfirm: async () => {
        try {
          await deleteOpening(opening.id)
          navigate(ensaioPath('/vagas'), { replace: true })
        } catch {
          return
        }
      },
    })

  const askWithdraw = () =>
    confirm({
      title: t('opening.withdrawTitle'),
      message: t('opening.withdrawDescription'),
      confirmLabel: t('opening.withdraw'),
      onConfirm: () => run(() => withdrawInterest(opening.id)),
    })

  return (
    <div className="app-page-enter mx-auto flex max-w-3xl flex-col gap-6">
      <PaperSheet className="flex flex-col gap-5">
        <div className="flex items-start justify-between gap-3">
          <div className="flex items-center gap-3">
            <AppAvatar name={opening.band.name} src={opening.band.avatarUrl} size="lg" verified={opening.band.isVerified} />
            <div className="flex flex-col">
              <span className="text-lg font-semibold">{opening.band.name}</span>
              {formatCity(opening) ? <span className="text-sm text-paper-muted">{formatCity(opening)}</span> : null}
            </div>
          </div>
          {opening.canManage ? (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <AppIconButton label={t('opening.actions')} className="text-paper-foreground hover:bg-paper-rule/20 hover:text-paper-foreground">
                  <EllipsisIcon />
                </AppIconButton>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-60">
                {opening.status === OpeningStatus.open ? (
                  <>
                    <DropdownMenuItem asChild>
                      <Link to={ensaioPath(`/vagas/${opening.id}/editar`)}>
                        <PencilIcon />
                        {t('opening.edit')}
                      </Link>
                    </DropdownMenuItem>
                    <DropdownMenuItem onSelect={() => void run(() => renewOpening(opening.id))}>
                      <RefreshCwIcon />
                      {t('opening.renew')}
                    </DropdownMenuItem>
                    <DropdownMenuItem onSelect={() => askClose(true)}>
                      <UserCheckIcon />
                      {t('opening.markFilled')}
                    </DropdownMenuItem>
                    <DropdownMenuItem onSelect={() => askClose(false)}>
                      <CircleSlashIcon />
                      {t('opening.close')}
                    </DropdownMenuItem>
                    <DropdownMenuSeparator />
                  </>
                ) : null}
                <DropdownMenuItem variant="destructive" onSelect={askDelete}>
                  <Trash2Icon />
                  {t('opening.delete')}
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          ) : null}
        </div>

        <div className="flex flex-col gap-2">
          <span className="font-typewriter text-paper-muted">{t('openings.wanted')}</span>
          <h1 className="font-heading text-5xl leading-none font-bold uppercase md:text-6xl">{label}</h1>
          <AppBadge tone={isOpen ? 'success' : 'neutral'} className="self-start">
            {t(`status.${status}`)}
          </AppBadge>
        </div>

        <p className="text-[17px] leading-relaxed whitespace-pre-line">{opening.description}</p>

        <dl className="flex flex-col gap-1 border-t-2 border-dashed border-paper-rule pt-4 text-sm">
          {opening.goal !== null ? (
            <div className="flex gap-2">
              <dt className="font-semibold">{t('opening.goal')}:</dt>
              <dd>{t(`goals.${goalKey[opening.goal]}`)}</dd>
            </div>
          ) : null}
          {opening.status === OpeningStatus.open ? (
            <dd className="text-paper-muted">
              {opening.isExpired ? t('opening.expiredHint') : t('opening.expiresAt', { date: new Date(opening.expiresAt).toLocaleDateString('pt-BR') })}
            </dd>
          ) : null}
        </dl>

        <InterestAction opening={opening} isOpen={isOpen} onInterest={() => setInterestOpen(true)} onWithdraw={askWithdraw} />
      </PaperSheet>

      <div className="flex flex-wrap gap-2">
        <AppButton asChild intent="secondary">
          {isExternalUrl(bandHref) ? <a href={bandHref}>{t('opening.seeBand')}</a> : <Link to={bandHref}>{t('opening.seeBand')}</Link>}
        </AppButton>
        <AppButton asChild intent="ghost">
          <Link to={ensaioPath('/vagas')}>{t('opening.back')}</Link>
        </AppButton>
      </div>

      {opening.canManage ? <InterestsList opening={opening} instrumentLabel={label} /> : null}

      <InterestDialog opening={interestOpen ? opening : null} onOpenChange={setInterestOpen} onSent={setOpening} />
    </div>
  )
}

function InterestAction({ opening, isOpen, onInterest, onWithdraw }: { opening: BandOpeningDto; isOpen: boolean; onInterest: () => void; onWithdraw: () => void }) {
  const { t } = useTranslation('ensaio')

  if (opening.canManage) return null

  if (opening.myInterest) {
    const hint =
      opening.myInterest.status === InterestStatus.invited
        ? t('opening.invitedHint')
        : opening.myInterest.status === InterestStatus.dismissed
          ? t('opening.dismissedHint')
          : null

    return (
      <div className="flex flex-col gap-2 border-t-2 border-dashed border-paper-rule pt-4">
        <span className="flex items-center gap-2 font-typewriter text-lg">
          <ScissorsIcon className="size-4" aria-hidden="true" />
          {t('openings.interestSent')}
        </span>
        {hint ? <p className="text-sm text-paper-muted">{hint}</p> : null}
        {opening.myInterest.status === InterestStatus.pending ? (
          <AppButton intent="ghost" size="sm" className="self-start text-paper-foreground hover:bg-paper-rule/20 hover:text-paper-foreground" onClick={onWithdraw}>
            {t('opening.withdraw')}
          </AppButton>
        ) : null}
      </div>
    )
  }

  if (!isOpen) return null

  return (
    <button
      type="button"
      onClick={onInterest}
      className="-mx-5 -mb-8 flex h-14 items-center justify-center gap-2 border-t-2 border-dashed border-paper-rule font-typewriter text-lg transition-colors hover:bg-paper-rule/15 focus-visible:bg-paper-rule/15 focus-visible:outline-none md:-mx-10 md:-mb-10"
    >
      <ScissorsIcon className="size-5" aria-hidden="true" />
      {t('openings.interested')}
    </button>
  )
}

function InterestsList({ opening, instrumentLabel }: { opening: BandOpeningDto; instrumentLabel: string }) {
  const { t } = useTranslation('ensaio')
  const confirm = useAppConfirm()
  const itemLabel = useInstrumentLabel()
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<OpeningInterestDto>>(() => emptyPage(interestsPageSize))
  const [loading, setLoading] = useState(true)
  const [version, setVersion] = useState(0)
  const [busy, setBusy] = useState<number | null>(null)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    getOpeningInterests(opening.id, { page, pageSize: interestsPageSize })
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .catch(() => undefined)
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [opening.id, page, version])

  const refresh = useCallback(() => setVersion((v) => v + 1), [])

  const act = async (interestId: number, action: () => Promise<unknown>) => {
    setBusy(interestId)
    try {
      await action()
      refresh()
    } catch {
      return
    } finally {
      setBusy(null)
    }
  }

  const askDismiss = (interest: OpeningInterestDto) =>
    confirm({
      title: t('interests.dismissTitle', { name: interest.musician.person.name }),
      message: t('interests.dismissDescription'),
      confirmLabel: t('interests.dismiss'),
      onConfirm: () => act(interest.id, () => dismissInterested(opening.id, interest.id)),
    })

  return (
    <section className="flex flex-col gap-4 rounded-xl border bg-card p-4 md:p-6">
      <h2 className="app-h2">{t('interests.title')}</h2>
      {loading ? (
        <AppSkeleton preset="list" count={3} />
      ) : result.items.length === 0 ? (
        <p className="text-muted-foreground">{t('interests.empty')}</p>
      ) : (
        <ul className="flex flex-col divide-y">
          {result.items.map((interest) => {
            const person = interest.musician.person
            const instrument = interest.musician.instrument
            return (
              <li key={interest.id} className="flex flex-col gap-3 py-4 sm:flex-row sm:items-start">
                <Link to={ensaioPath(`/musicos/${person.nickname}`)} className="flex min-w-0 flex-1 gap-3">
                  <AppAvatar name={person.name} src={person.avatarUrl} size="lg" />
                  <span className="flex min-w-0 flex-col gap-0.5">
                    <span className="font-semibold">{person.name}</span>
                    <span className="app-meta">
                      {[instrument ? itemLabel(instrument.instrument, instrument.otherName) : null, formatCity(interest.musician)].filter(Boolean).join(' · ')}
                    </span>
                    {interest.message ? <span className="mt-1 text-sm whitespace-pre-line">“{interest.message}”</span> : null}
                  </span>
                </Link>
                <div className="flex shrink-0 items-center gap-2">
                  <AppBadge tone={interestTone[interest.status]}>{t(`interestStatus.${interestStatusKey[interest.status]}`)}</AppBadge>
                  {interest.status === InterestStatus.pending ? (
                    <>
                      <AppButton size="sm" loading={busy === interest.id} onClick={() => void act(interest.id, () => inviteInterested(opening.id, interest.id, instrumentLabel))}>
                        {t('interests.invite')}
                      </AppButton>
                      <AppButton size="sm" intent="ghost" disabled={busy === interest.id} onClick={() => askDismiss(interest)}>
                        {t('interests.dismiss')}
                      </AppButton>
                    </>
                  ) : null}
                </div>
              </li>
            )
          })}
        </ul>
      )}
      <AppPagination page={page} totalPages={result.totalPages} totalCount={result.totalCount} onChange={setPage} loading={loading} />
    </section>
  )
}
