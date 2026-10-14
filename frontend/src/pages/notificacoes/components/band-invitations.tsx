import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router-dom'

import { AppAvatar } from '@/components/app-avatar'
import { AppButton } from '@/components/app-button'
import { useNotifications } from '@/layout/use-notifications'
import { formatDate } from '@/lib/format'
import { acceptBandInvitation, declineBandInvitation, getMyBandInvitations } from '@/pages/bandas/services/bandas-service'
import type { BandInvitationDto } from '@/pages/bandas/services/bandas-service'

const pageSize = 20

export function BandInvitations() {
  const { t } = useTranslation('notificacoes')
  const navigate = useNavigate()
  const [invitations, setInvitations] = useState<BandInvitationDto[]>([])
  const [busyId, setBusyId] = useState<number | null>(null)
  const [version, setVersion] = useState(0)
  const { version: notificationsVersion } = useNotifications()

  useEffect(() => {
    let cancelled = false
    void getMyBandInvitations({ page: 1, pageSize }).then((page) => {
      if (!cancelled) setInvitations(page.items)
    })
    return () => {
      cancelled = true
    }
  }, [version, notificationsVersion])

  const refresh = useCallback(() => setVersion((v) => v + 1), [])

  const accept = async (invitation: BandInvitationDto) => {
    setBusyId(invitation.id)
    try {
      await acceptBandInvitation(invitation.id)
      navigate(`/bandas/${invitation.band.slug}`)
    } catch {
      refresh()
    } finally {
      setBusyId(null)
    }
  }

  const decline = async (invitation: BandInvitationDto) => {
    setBusyId(invitation.id)
    try {
      await declineBandInvitation(invitation.id)
    } catch {
      return
    } finally {
      setBusyId(null)
      refresh()
    }
  }

  if (invitations.length === 0) return null

  return (
    <section className="mb-6 flex flex-col gap-3">
      <h2 className="app-h3">{t('invitations.title')}</h2>
      <ul className="flex flex-col divide-y rounded-xl border border-primary/40 bg-card">
        {invitations.map((invitation) => (
          <li key={invitation.id} className="flex flex-col gap-3 p-4 sm:flex-row sm:items-center">
            <Link to={`/bandas/${invitation.band.slug}`} className="shrink-0">
              <AppAvatar name={invitation.band.name} src={invitation.band.avatarUrl} size="lg" verified={invitation.band.isVerified} />
            </Link>
            <div className="flex min-w-0 flex-1 flex-col gap-0.5">
              <p className="text-sm font-medium">
                {invitation.instrument
                  ? t('invitations.messageInstrument', { name: invitation.invitedBy.name, instrument: invitation.instrument, band: invitation.band.name })
                  : t('invitations.message', { name: invitation.invitedBy.name, band: invitation.band.name })}
              </p>
              <span className="app-meta">{t('invitations.expires', { date: formatDate(invitation.expiresAt) })}</span>
            </div>
            <div className="flex gap-2">
              <AppButton size="sm" loading={busyId === invitation.id} disabled={busyId !== null} onClick={() => void accept(invitation)}>
                {t('invitations.accept')}
              </AppButton>
              <AppButton size="sm" intent="neutral" disabled={busyId !== null} onClick={() => void decline(invitation)}>
                {t('invitations.decline')}
              </AppButton>
            </div>
          </li>
        ))}
      </ul>
    </section>
  )
}
