import { useState } from 'react'

import { lastVisitedPath } from '@/layout/last-location'

import type { ReportAttachment } from '../components/report-attachments'
import { createTicket } from '../services/suporte-service'
import type { SupportTicketDto } from '../services/suporte-service'
import type { SupportArea, SupportTag } from '../support-enums'

export type ReportPayload = {
  title: string
  description: string
  tags: SupportTag[]
  areas: SupportArea[]
  attachments: ReportAttachment[]
}

export function useReportSubmit() {
  const [origin] = useState(() => lastVisitedPath())
  const [submitting, setSubmitting] = useState(false)
  const [created, setCreated] = useState<SupportTicketDto | null>(null)

  const submit = async (payload: ReportPayload) => {
    setSubmitting(true)
    try {
      const ticket = await createTicket({
        title: payload.title,
        description: payload.description,
        tags: payload.tags,
        areas: payload.areas,
        mediaIds: payload.attachments.filter((item) => item.state === 'ready' && item.mediaId !== null).map((item) => item.mediaId!),
        originPath: origin,
        userAgent: navigator.userAgent,
        viewportWidth: window.innerWidth,
        viewportHeight: window.innerHeight,
      })
      setCreated(ticket)
    } finally {
      setSubmitting(false)
    }
  }

  return { origin, submitting, created, submit }
}
