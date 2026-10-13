import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'

import { AppCard } from '@/components/app-card'
import { AppPageHeader } from '@/components/app-page-header'

import { emptyBandForm, instrumentOf, toBandInput } from './band-validators'
import type { BandFormValues } from './band-validators'
import { BandForm } from './components/band-form'
import { createBand } from './services/bandas-service'

export function BandaNovaPage() {
  const { t } = useTranslation('bandas')
  const navigate = useNavigate()
  const [saving, setSaving] = useState(false)

  const submit = async (values: BandFormValues) => {
    setSaving(true)
    try {
      const band = await createBand({ ...toBandInput(values), instrument: instrumentOf(values) })
      navigate(`/bandas/${band.slug}/editar`, { replace: true })
    } catch {
      return
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="app-page-enter mx-auto flex max-w-2xl flex-col gap-6">
      <AppPageHeader title={t('create.title')} description={t('create.description')} />
      <AppCard>
        <BandForm defaultValues={emptyBandForm} withInstrument submitLabel={t('create.submit')} saving={saving} onSubmit={submit} />
      </AppCard>
    </div>
  )
}
