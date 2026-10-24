import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'

import { AppPageHeader } from '@/components/app-page-header'

import { bandEditPath, bandKindContext } from './band-kind'
import type { BandKind } from './band-kind'
import { emptyBandForm, instrumentOf, toBandInput } from './band-validators'
import type { BandFormValues } from './band-validators'
import { BandForm } from './components/band-form'
import { createBand } from './services/bandas-service'

export function BandaNovaPage({ kind }: { kind: BandKind }) {
  const { t } = useTranslation('bandas')
  const navigate = useNavigate()
  const [saving, setSaving] = useState(false)
  const context = bandKindContext(kind)

  const submit = async (values: BandFormValues) => {
    setSaving(true)
    try {
      const band = await createBand({ ...toBandInput(values), kind, instrument: instrumentOf(values) })
      navigate(bandEditPath({ slug: band.slug, kind: band.kind ?? kind }), { replace: true })
    } catch {
      return
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="app-page-enter flex flex-col gap-6">
      <AppPageHeader title={t('create.title', { context })} description={t('create.description', { context })} />
      <BandForm
        key={kind}
        defaultValues={{ ...emptyBandForm, kind }}
        withInstrument
        submitLabel={t('create.submit', { context })}
        saving={saving}
        onSubmit={submit}
        onCancel={() => navigate(-1)}
      />
    </div>
  )
}
