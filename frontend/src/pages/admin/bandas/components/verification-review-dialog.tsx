import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { AppAvatar } from '@/components/app-avatar'
import { AppBadge } from '@/components/app-badge'
import { AppButton } from '@/components/app-button'
import { useAppConfirm } from '@/components/app-confirm'
import { AppDialog } from '@/components/app-dialog'
import { AppSocialLinks } from '@/components/app-social-links'
import { formatDateTime } from '@/lib/format'
import { BandVerificationStatus, bandVerificationStatusKey, bandVerificationStatusTone } from '@/pages/bandas/band-verification-status'
import type { BandVerificationDto } from '@/pages/bandas/services/bandas-service'

import { approveVerification, bandVerificationReasonMaxLength, bandVerificationReasonMinLength, rejectVerification, unverifyBand } from '../services/bandas-admin-service'

type VerificationReviewDialogProps = {
  request: BandVerificationDto | null
  canVerify: boolean
  onOpenChange: (open: boolean) => void
  onChanged: () => void
}

export function VerificationReviewDialog({ request, canVerify, onOpenChange, onChanged }: VerificationReviewDialogProps) {
  const { t } = useTranslation('bandas')
  const confirm = useAppConfirm()
  const [reason, setReason] = useState('')
  const [rejecting, setRejecting] = useState(false)
  const [busy, setBusy] = useState(false)

  const change = (open: boolean) => {
    if (!open) {
      setReason('')
      setRejecting(false)
    }
    onOpenChange(open)
  }

  const run = async (action: () => Promise<unknown>) => {
    setBusy(true)
    try {
      await action()
      onChanged()
      change(false)
    } catch {
      return
    } finally {
      setBusy(false)
    }
  }

  if (!request) return null

  const pending = request.status === BandVerificationStatus.pending
  const reasonValue = reason.trim()
  const reasonValid = reasonValue.length >= bandVerificationReasonMinLength && reasonValue.length <= bandVerificationReasonMaxLength

  const approve = () =>
    confirm({
      title: t('admin.approveTitle', { name: request.band.name }),
      message: t('admin.approveMessage'),
      confirmLabel: t('admin.approveConfirm'),
      onConfirm: () => run(() => approveVerification(request.id)),
    })

  const unverify = () =>
    confirm({
      title: t('admin.unverifyTitle', { name: request.band.name }),
      message: t('admin.unverifyMessage'),
      confirmLabel: t('admin.unverifyConfirm'),
      danger: true,
      onConfirm: () => run(() => unverifyBand(request.band.id)),
    })

  return (
    <AppDialog open onOpenChange={change} title={t('admin.reviewTitle')} description={t('admin.reviewDescription')} size="lg">
      <div className="flex flex-col gap-5">
        <div className="flex items-center gap-3">
          <AppAvatar name={request.band.name} src={request.band.avatarUrl} size="lg" verified={request.band.isVerified} />
          <div className="flex min-w-0 flex-1 flex-col">
            <Link to={`/bandas/${request.band.slug}`} target="_blank" rel="noopener noreferrer" className="truncate font-heading text-lg font-semibold tracking-wide uppercase hover:underline">
              {request.band.name}
            </Link>
            <span className="app-meta">
              {t('admin.memberCount', { count: request.bandMemberCount })} · {t('admin.requestedAt', { date: formatDateTime(request.createdAt) })}
            </span>
          </div>
          <AppBadge tone={bandVerificationStatusTone[request.status]}>{t(`verification.status.${bandVerificationStatusKey[request.status]}`)}</AppBadge>
        </div>

        <div className="flex flex-wrap items-center gap-3">
          <AppSocialLinks links={request.bandSocialLinks} />
          <AppButton intent="ghost" size="sm" asChild>
            <Link to={`/bandas/${request.band.slug}`} target="_blank" rel="noopener noreferrer">
              {t('admin.openBand')}
            </Link>
          </AppButton>
        </div>

        <div className="flex flex-col gap-2 rounded-xl border bg-card p-4">
          <div className="flex items-center gap-2">
            <AppAvatar name={request.requestedBy.name} src={request.requestedBy.avatarUrl} size="sm" />
            <Link to={`/perfil/${request.requestedBy.nickname}`} className="text-sm font-medium hover:underline">
              {request.requestedBy.name}
            </Link>
            <span className="app-meta">@{request.requestedBy.nickname}</span>
          </div>
          <p className="text-sm leading-relaxed whitespace-pre-line">{request.message}</p>
          <span className="app-meta">{request.contactPhone ? t('admin.contact', { phone: request.contactPhone }) : t('admin.noContact')}</span>
        </div>

        {request.status !== BandVerificationStatus.pending && request.reviewedBy ? (
          <div className="flex flex-col gap-1 rounded-xl border border-dashed p-4 text-sm">
            <span className="app-meta">
              {t('admin.reviewedBy', { name: request.reviewedBy.name, date: request.reviewedAt ? formatDateTime(request.reviewedAt) : '' })}
            </span>
            {request.rejectionReason ? <p className="whitespace-pre-line">{request.rejectionReason}</p> : null}
          </div>
        ) : null}

        {pending && canVerify ? (
          rejecting ? (
            <div className="flex flex-col gap-3">
              <label className="flex flex-col gap-1.5 text-sm">
                <span className="font-medium">{t('admin.reasonLabel')}</span>
                <textarea
                  value={reason}
                  onChange={(event) => setReason(event.target.value)}
                  rows={4}
                  maxLength={bandVerificationReasonMaxLength}
                  placeholder={t('admin.reasonPlaceholder')}
                  className="w-full resize-y rounded-lg border bg-background px-3 py-2 text-[15px] outline-none focus-visible:ring-2 focus-visible:ring-ring/50"
                />
                <span className="app-meta">{t('admin.reasonHint', { min: bandVerificationReasonMinLength, max: bandVerificationReasonMaxLength })}</span>
              </label>
              <div className="flex justify-end gap-2">
                <AppButton type="button" intent="ghost" onClick={() => setRejecting(false)} disabled={busy}>
                  {t('admin.back')}
                </AppButton>
                <AppButton type="button" intent="danger" loading={busy} disabled={!reasonValid} onClick={() => void run(() => rejectVerification(request.id, reasonValue))}>
                  {t('admin.rejectConfirm')}
                </AppButton>
              </div>
            </div>
          ) : (
            <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
              <AppButton type="button" intent="danger" onClick={() => setRejecting(true)} disabled={busy}>
                {t('admin.reject')}
              </AppButton>
              <AppButton type="button" loading={busy} onClick={approve}>
                {t('admin.approve')}
              </AppButton>
            </div>
          )
        ) : null}

        {request.status === BandVerificationStatus.approved && request.band.isVerified && canVerify ? (
          <div className="flex justify-end">
            <AppButton type="button" intent="danger" onClick={unverify} disabled={busy}>
              {t('admin.unverify')}
            </AppButton>
          </div>
        ) : null}
      </div>
    </AppDialog>
  )
}
