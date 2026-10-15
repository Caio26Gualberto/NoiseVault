import { zodResolver } from '@hookform/resolvers/zod'
import { MailPlusIcon, XIcon } from 'lucide-react'
import { useCallback, useEffect, useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'

import { AppAvatar } from '@/components/app-avatar'
import { AppButton } from '@/components/app-button'
import { AppCard } from '@/components/app-card'
import { useAppConfirm } from '@/components/app-confirm'
import { AppField, AppForm } from '@/components/app-field'
import { useDebouncedValue } from '@/hooks/use-debounced-value'
import { formatDate } from '@/lib/format'
import { instrumentMaxLength } from '@/pages/meu-perfil/profile-validators'
import { peopleSearchMinLength, searchPeople } from '@/pages/perfil/services/perfil-service'
import type { ProfileSummaryDto } from '@/pages/perfil/services/perfil-service'

import { getBandInvitations, inviteBandMember, revokeBandInvitation } from '../services/bandas-service'
import type { BandInvitationDto, BandProfileDto } from '../services/bandas-service'

const pendingPageSize = 20

const inviteSchema = z.object({
  nickname: z.string().trim().min(1, 'validation.required'),
  instrument: z.string().trim().max(instrumentMaxLength, 'validation.instrumentLength'),
})

type InviteValues = z.infer<typeof inviteSchema>

export function InvitationsManager({ band }: { band: BandProfileDto }) {
  const { t } = useTranslation('bandas')
  const confirm = useAppConfirm()
  const [pending, setPending] = useState<BandInvitationDto[]>([])
  const [version, setVersion] = useState(0)
  const [selected, setSelected] = useState<ProfileSummaryDto | null>(null)
  const [suggestions, setSuggestions] = useState<ProfileSummaryDto[]>([])
  const [sending, setSending] = useState(false)

  const form = useForm<InviteValues>({
    resolver: zodResolver(inviteSchema),
    defaultValues: { nickname: '', instrument: '' },
  })

  const term = useDebouncedValue(useWatch({ control: form.control, name: 'nickname' }).trim())

  useEffect(() => {
    let cancelled = false
    void getBandInvitations(band.slug, { page: 1, pageSize: pendingPageSize }).then((page) => {
      if (!cancelled) setPending(page.items)
    })
    return () => {
      cancelled = true
    }
  }, [band.slug, version])

  useEffect(() => {
    if (selected || term.length < peopleSearchMinLength) return
    let cancelled = false
    const memberIds = new Set(band.members.map((m) => m.user.id))
    searchPeople(term.replace(/^@/, ''), 6)
      .then((page) => {
        if (!cancelled) setSuggestions(page.items.filter((person) => !memberIds.has(person.id)))
      })
      .catch(() => {
        if (!cancelled) setSuggestions([])
      })
    return () => {
      cancelled = true
    }
  }, [term, selected, band.members])

  const refresh = useCallback(() => setVersion((v) => v + 1), [])

  const pick = (person: ProfileSummaryDto) => {
    setSelected(person)
    setSuggestions([])
    form.setValue('nickname', person.nickname, { shouldValidate: true })
  }

  const clear = () => {
    setSelected(null)
    form.setValue('nickname', '')
  }

  const send = async (values: InviteValues) => {
    setSending(true)
    try {
      await inviteBandMember(band.slug, values.nickname.replace(/^@/, ''), values.instrument || null)
      form.reset({ nickname: '', instrument: '' })
      setSelected(null)
      refresh()
    } catch {
      return
    } finally {
      setSending(false)
    }
  }

  const revoke = (invitation: BandInvitationDto) =>
    confirm({
      title: t('invite.revokeTitle', { name: invitation.invitedUser.name }),
      message: t('invite.revokeMessage'),
      confirmLabel: t('invite.revokeConfirm'),
      danger: true,
      onConfirm: async () => {
        await revokeBandInvitation(invitation.id)
        refresh()
      },
    })

  const showSuggestions = !selected && term.length >= peopleSearchMinLength

  return (
    <AppCard title={t('invite.title')} description={t('invite.description')}>
      <div className="flex flex-col gap-6">
        <AppForm form={form} onSubmit={send}>
          {selected ? (
            <div className="flex flex-col gap-1.5">
              <span className="text-sm font-medium">{t('invite.selected')}</span>
              <div className="flex items-center gap-3 rounded-lg border p-2">
                <AppAvatar name={selected.name} src={selected.avatarUrl} size="md" />
                <span className="flex min-w-0 flex-1 flex-col">
                  <span className="truncate font-medium">{selected.name}</span>
                  <span className="app-meta truncate">@{selected.nickname}</span>
                </span>
                <AppButton size="sm" intent="ghost" onClick={clear}>
                  <XIcon />
                  {t('invite.change')}
                </AppButton>
              </div>
            </div>
          ) : (
            <div className="flex flex-col gap-2">
              <AppField name="nickname" label={t('invite.search')} placeholder={t('invite.searchPlaceholder')} autoComplete="off" />
              {showSuggestions ? (
                suggestions.length > 0 ? (
                  <ul className="flex flex-col overflow-hidden rounded-lg border">
                    {suggestions.map((person) => (
                      <li key={person.id}>
                        <button
                          type="button"
                          onClick={() => pick(person)}
                          className="flex w-full items-center gap-3 px-3 py-2 text-left transition-colors hover:bg-accent focus-visible:bg-accent focus-visible:outline-none"
                        >
                          <AppAvatar name={person.name} src={person.avatarUrl} size="sm" />
                          <span className="truncate text-sm font-medium">{person.name}</span>
                          <span className="app-meta truncate">@{person.nickname}</span>
                        </button>
                      </li>
                    ))}
                  </ul>
                ) : (
                  <p className="app-meta">{t('invite.searchEmpty')}</p>
                )
              ) : null}
            </div>
          )}
          <AppField name="instrument" label={t('invite.instrument')} placeholder={t('invite.instrumentPlaceholder')} />
          <AppButton type="submit" loading={sending} className="self-start">
            <MailPlusIcon />
            {t('invite.submit')}
          </AppButton>
        </AppForm>

        <div className="flex flex-col gap-2">
          <h3 className="app-h3">{t('invite.pending')}</h3>
          {pending.length === 0 ? (
            <p className="app-meta">{t('invite.none')}</p>
          ) : (
            <ul className="flex flex-col divide-y">
              {pending.map((invitation) => (
                <li key={invitation.id} className="flex items-center gap-3 py-2">
                  <AppAvatar name={invitation.invitedUser.name} src={invitation.invitedUser.avatarUrl} size="md" />
                  <span className="flex min-w-0 flex-1 flex-col">
                    <span className="truncate font-medium">{invitation.invitedUser.name}</span>
                    <span className="app-meta truncate">
                      {invitation.instrument ? `${invitation.instrument} · ` : ''}
                      {t('invite.expires', { date: formatDate(invitation.expiresAt) })}
                    </span>
                  </span>
                  <AppButton size="sm" intent="ghost" onClick={() => revoke(invitation)}>
                    {t('invite.revoke')}
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
