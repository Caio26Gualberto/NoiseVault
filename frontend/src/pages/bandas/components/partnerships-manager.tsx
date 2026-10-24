import { CheckIcon, HandshakeIcon, XIcon } from 'lucide-react'
import { useCallback, useEffect, useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { AppAvatar } from '@/components/app-avatar'
import { AppBadge } from '@/components/app-badge'
import { AppButton } from '@/components/app-button'
import { AppCard } from '@/components/app-card'
import { useAppConfirm } from '@/components/app-confirm'
import { AppField, AppForm } from '@/components/app-field'
import { useDebouncedValue } from '@/hooks/use-debounced-value'
import { formatDate } from '@/lib/format'

import { bandKindKey, bandPagePath } from '../band-kind'
import { BandPartnershipDirection, BandPartnershipStatus } from '../band-partnership-status'
import {
  acceptBandPartnership,
  bandSearchMinLength,
  declineBandPartnership,
  getBandPartners,
  getPendingBandPartnerships,
  inviteBandPartner,
  removeBandPartnership,
  searchBands,
} from '../services/bandas-service'
import type { BandPartnershipDto, BandProfileDto, BandSummaryDto } from '../services/bandas-service'

export const partnersPageSize = 50

export function PartnershipsManager({ band }: { band: BandProfileDto }) {
  const { t } = useTranslation('bandas')
  const confirm = useAppConfirm()
  const [partners, setPartners] = useState<BandPartnershipDto[]>([])
  const [pending, setPending] = useState<BandPartnershipDto[]>([])
  const [version, setVersion] = useState(0)
  const form = useForm<{ search: string }>({ defaultValues: { search: '' } })
  const [suggestions, setSuggestions] = useState<BandSummaryDto[]>([])
  const [sendingTo, setSendingTo] = useState<number | null>(null)
  const [answering, setAnswering] = useState<number | null>(null)
  const term = useDebouncedValue(useWatch({ control: form.control, name: 'search' }).trim())

  useEffect(() => {
    let cancelled = false
    void Promise.all([
      getBandPartners(band.slug, { page: 1, pageSize: partnersPageSize }),
      getPendingBandPartnerships(band.slug, { page: 1, pageSize: partnersPageSize }),
    ]).then(([accepted, open]) => {
      if (cancelled) return
      setPartners(accepted.items)
      setPending(open.items)
    })
    return () => {
      cancelled = true
    }
  }, [band.slug, version])

  useEffect(() => {
    if (term.length < bandSearchMinLength) return
    let cancelled = false
    searchBands(term, 6)
      .then((page) => {
        if (!cancelled) setSuggestions(page.items.filter((item) => item.id !== band.id))
      })
      .catch(() => {
        if (!cancelled) setSuggestions([])
      })
    return () => {
      cancelled = true
    }
  }, [term, band.id])

  const refresh = useCallback(() => setVersion((v) => v + 1), [])

  const related = new Set([...partners, ...pending.filter((p) => p.status === BandPartnershipStatus.pending)].map((p) => p.partner.id))

  const invite = async (target: BandSummaryDto) => {
    setSendingTo(target.id)
    try {
      await inviteBandPartner(band.slug, target.slug)
      form.reset({ search: '' })
      setSuggestions([])
      refresh()
    } catch {
      return
    } finally {
      setSendingTo(null)
    }
  }

  const answer = async (partnership: BandPartnershipDto, accept: boolean) => {
    setAnswering(partnership.id)
    try {
      await (accept ? acceptBandPartnership(partnership.id) : declineBandPartnership(partnership.id))
      refresh()
    } catch {
      return
    } finally {
      setAnswering(null)
    }
  }

  const remove = (partnership: BandPartnershipDto, pendingInvite: boolean) =>
    confirm({
      title: t(pendingInvite ? 'partners.cancelTitle' : 'partners.removeTitle', { name: partnership.partner.name }),
      message: t(pendingInvite ? 'partners.cancelMessage' : 'partners.removeMessage'),
      confirmLabel: t(pendingInvite ? 'partners.cancelConfirm' : 'partners.removeConfirm'),
      danger: true,
      onConfirm: async () => {
        await removeBandPartnership(partnership.id)
        refresh()
      },
    })

  const incoming = pending.filter((p) => p.direction === BandPartnershipDirection.incoming)
  const outgoing = pending.filter((p) => p.direction === BandPartnershipDirection.outgoing)
  const showSuggestions = term.length >= bandSearchMinLength

  return (
    <AppCard title={t('partners.title')} description={t('partners.manageDescription')}>
      <div className="flex flex-col gap-6">
        {incoming.length > 0 ? (
          <div className="flex flex-col gap-2">
            <h3 className="app-h3">{t('partners.incoming')}</h3>
            <ul className="flex flex-col divide-y">
              {incoming.map((partnership) => (
                <li key={partnership.id} className="flex flex-wrap items-center gap-3 py-2">
                  <PartnerLine partner={partnership.partner} meta={t('partners.expires', { date: formatDate(partnership.expiresAt) })} />
                  <div className="flex gap-2">
                    <AppButton size="sm" loading={answering === partnership.id} onClick={() => void answer(partnership, true)}>
                      <CheckIcon />
                      {t('partners.accept')}
                    </AppButton>
                    <AppButton size="sm" intent="ghost" disabled={answering === partnership.id} onClick={() => void answer(partnership, false)}>
                      {t('partners.decline')}
                    </AppButton>
                  </div>
                </li>
              ))}
            </ul>
          </div>
        ) : null}

        <AppForm form={form} onSubmit={() => undefined}>
          <AppField name="search" label={t('partners.search')} placeholder={t('partners.searchPlaceholder')} autoComplete="off" />
          {showSuggestions ? (
            suggestions.length > 0 ? (
              <ul className="flex flex-col overflow-hidden rounded-lg border">
                {suggestions.map((item) => (
                  <li key={item.id} className="flex items-center gap-3 px-3 py-2">
                    <PartnerLine partner={item} />
                    {related.has(item.id) ? (
                      <span className="app-meta">{t('partners.alreadyRelated')}</span>
                    ) : (
                      <AppButton type="button" size="sm" intent="secondary" loading={sendingTo === item.id} onClick={() => void invite(item)}>
                        <HandshakeIcon />
                        {t('partners.invite')}
                      </AppButton>
                    )}
                  </li>
                ))}
              </ul>
            ) : (
              <p className="app-meta">{t('partners.searchEmpty')}</p>
            )
          ) : null}
          <p className="app-meta">{t('partners.inviteHint')}</p>
        </AppForm>

        {outgoing.length > 0 ? (
          <div className="flex flex-col gap-2">
            <h3 className="app-h3">{t('partners.outgoing')}</h3>
            <ul className="flex flex-col divide-y">
              {outgoing.map((partnership) => (
                <li key={partnership.id} className="flex items-center gap-3 py-2">
                  <PartnerLine
                    partner={partnership.partner}
                    meta={
                      partnership.status === BandPartnershipStatus.declined
                        ? t('partners.notAccepted')
                        : t('partners.expires', { date: formatDate(partnership.expiresAt) })
                    }
                  />
                  {partnership.status === BandPartnershipStatus.pending ? (
                    <AppButton size="sm" intent="ghost" onClick={() => remove(partnership, true)}>
                      {t('partners.cancel')}
                    </AppButton>
                  ) : (
                    <AppBadge tone="neutral">{t('partners.notAcceptedBadge')}</AppBadge>
                  )}
                </li>
              ))}
            </ul>
          </div>
        ) : null}

        <div className="flex flex-col gap-2">
          <h3 className="app-h3">{t('partners.current')}</h3>
          {partners.length === 0 ? (
            <p className="app-meta">{t('partners.none')}</p>
          ) : (
            <ul className="flex flex-col divide-y">
              {partners.map((partnership) => (
                <li key={partnership.id} className="flex items-center gap-3 py-2">
                  <PartnerLine partner={partnership.partner} link />
                  <AppButton size="sm" intent="ghost" onClick={() => remove(partnership, false)}>
                    <XIcon />
                    {t('partners.remove')}
                  </AppButton>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </AppCard>
  )
}

function PartnerLine({ partner, meta, link = false }: { partner: BandSummaryDto; meta?: string; link?: boolean }) {
  const { t } = useTranslation('bandas')
  const kind = t(`kinds.${bandKindKey[partner.kind ?? 0]}.title`)
  const content = (
    <>
      <AppAvatar name={partner.name} src={partner.avatarUrl} size="md" verified={partner.isVerified} />
      <span className="flex min-w-0 flex-1 flex-col">
        <span className="truncate font-medium">{partner.name}</span>
        <span className="app-meta truncate">{meta ? `${kind} · ${meta}` : kind}</span>
      </span>
    </>
  )

  return link ? (
    <Link to={bandPagePath(partner)} className="flex min-w-0 flex-1 items-center gap-3 hover:underline">
      {content}
    </Link>
  ) : (
    <span className="flex min-w-0 flex-1 items-center gap-3">{content}</span>
  )
}
