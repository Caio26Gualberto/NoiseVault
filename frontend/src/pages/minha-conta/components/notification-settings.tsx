import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useAuth } from '@/auth/use-auth'
import { AppCard } from '@/components/app-card'
import { Checkbox } from '@/components/ui/checkbox'

import { updateNotificationSettings } from '../services/conta-service'

export function NotificationSettings() {
  const { t } = useTranslation('conta')
  const { user, replaceUser } = useAuth()
  const [saving, setSaving] = useState(false)

  if (!user) return null

  const toggle = async (checked: boolean) => {
    setSaving(true)
    try {
      replaceUser(await updateNotificationSettings({ emailOnBandInvite: checked }))
    } catch {
      return
    } finally {
      setSaving(false)
    }
  }

  return (
    <AppCard title={t('notifications.title')} description={t('notifications.description')}>
      <label className="flex items-start gap-3 text-sm">
        <Checkbox
          checked={user.emailOnBandInvite}
          disabled={saving}
          onCheckedChange={(checked) => void toggle(checked === true)}
          className="mt-0.5"
        />
        <span className="flex flex-col gap-0.5">
          <span className="font-medium">{t('notifications.emailOnBandInvite')}</span>
          <span className="app-meta">{t('notifications.emailOnBandInviteHint')}</span>
        </span>
      </label>
    </AppCard>
  )
}
