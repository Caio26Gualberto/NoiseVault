import { LogOutIcon, ShieldCheckIcon, UserMinusIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { useAuth } from '@/auth/use-auth'
import { AppAvatar } from '@/components/app-avatar'
import { AppBadge } from '@/components/app-badge'
import { AppButton } from '@/components/app-button'
import { AppCard } from '@/components/app-card'
import { useAppConfirm } from '@/components/app-confirm'

import { BandMemberRole } from '../band-member-role'
import { leaveBand, promoteBandMember, removeBandMember } from '../services/bandas-service'
import type { BandMemberDto, BandProfileDto } from '../services/bandas-service'

type MembersManagerProps = {
  band: BandProfileDto
  onChange: (band: BandProfileDto) => void
  onLeft: () => void
}

export function MembersManager({ band, onChange, onLeft }: MembersManagerProps) {
  const { t } = useTranslation('bandas')
  const { user } = useAuth()
  const confirm = useAppConfirm()

  const promote = (member: BandMemberDto) =>
    confirm({
      title: t('manage.promoteTitle', { name: member.user.name }),
      message: t('manage.promoteMessage'),
      confirmLabel: t('manage.promoteConfirm'),
      onConfirm: async () => onChange(await promoteBandMember(band.slug, member.user.id)),
    })

  const remove = (member: BandMemberDto) =>
    confirm({
      title: t('manage.removeTitle', { name: member.user.name }),
      message: t('manage.removeMessage'),
      confirmLabel: t('manage.removeConfirm'),
      danger: true,
      onConfirm: async () => onChange(await removeBandMember(band.slug, member.user.id)),
    })

  const leave = () =>
    confirm({
      title: t('leave.title', { name: band.name }),
      message: t('leave.message'),
      confirmLabel: t('leave.confirm'),
      danger: true,
      onConfirm: async () => {
        await leaveBand(band.slug)
        onLeft()
      },
    })

  return (
    <AppCard title={t('manage.title')} description={t('manage.description')}>
      <ul className="flex flex-col divide-y">
        {band.members.map((member) => {
          const isMe = member.user.id === user?.id
          const isOwner = member.role === BandMemberRole.owner
          return (
            <li key={member.user.id} className="flex flex-col gap-3 py-3 first:pt-0 last:pb-0 sm:flex-row sm:items-center">
              <Link to={`/perfil/${member.user.nickname}`} className="flex min-w-0 flex-1 items-center gap-3 hover:underline">
                <AppAvatar name={member.user.name} src={member.user.avatarUrl} size="md" />
                <span className="flex min-w-0 flex-col">
                  <span className="truncate font-medium">
                    {member.user.name}
                    {isMe ? <span className="app-meta"> ({t('you')})</span> : null}
                  </span>
                  <span className="app-meta truncate">@{member.user.nickname}{member.instrument ? ` · ${member.instrument}` : ''}</span>
                </span>
              </Link>
              <div className="flex flex-wrap items-center gap-2">
                {isOwner ? <AppBadge tone="brand">{t('owner')}</AppBadge> : null}
                {isMe ? (
                  <AppButton size="sm" intent="ghost" onClick={leave}>
                    <LogOutIcon />
                    {t('leave.action')}
                  </AppButton>
                ) : !isOwner ? (
                  <>
                    <AppButton size="sm" intent="neutral" onClick={() => promote(member)}>
                      <ShieldCheckIcon />
                      {t('manage.promote')}
                    </AppButton>
                    <AppButton size="sm" intent="ghost" onClick={() => remove(member)}>
                      <UserMinusIcon />
                      {t('manage.remove')}
                    </AppButton>
                  </>
                ) : null}
              </div>
            </li>
          )
        })}
      </ul>
    </AppCard>
  )
}
