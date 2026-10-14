import { useCallback, useEffect, useState } from 'react'

import { ApiCodes } from '@/api/api-codes'
import { feedback } from '@/api/feedback-sink'
import { ApiError, emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { maxPageSize } from '@/components/app-pagination'

import {
  addEventComment,
  addInterest,
  cancelEvent,
  deleteEventComment,
  getEvent,
  getEventComments,
  removeInterest,
} from '../services/eventos-service'
import type { EventCommentDto, EventDto } from '../services/eventos-service'

export type EventState = { status: 'loading' } | { status: 'found'; event: EventDto } | { status: 'notFound' } | { status: 'failed' }

export function useEventDetail(eventId: number) {
  const [state, setState] = useState<EventState>({ status: 'loading' })
  const [comments, setComments] = useState<PagedResult<EventCommentDto>>(() => emptyPage(maxPageSize))
  const [commentPage, setCommentPage] = useState(1)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let cancelled = false
    getEvent(eventId)
      .then((event) => {
        if (!cancelled) setState({ status: 'found', event })
      })
      .catch((error: unknown) => {
        if (cancelled) return
        if (error instanceof ApiError && error.code === ApiCodes.eventNotFound) {
          setState({ status: 'notFound' })
          return
        }
        if (error instanceof ApiError) feedback.error(error.code, error.backendMessage)
        setState({ status: 'failed' })
      })
    return () => {
      cancelled = true
    }
  }, [eventId, version])

  useEffect(() => {
    let cancelled = false
    getEventComments(eventId, { page: commentPage, pageSize: maxPageSize })
      .then((data) => {
        if (cancelled) return
        setComments((previous) => (commentPage === 1 ? data : { ...data, items: [...previous.items, ...data.items] }))
      })
      .catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [eventId, commentPage, version])

  const reload = useCallback(() => {
    setCommentPage(1)
    setVersion((value) => value + 1)
  }, [])

  const replace = useCallback((event: EventDto) => setState({ status: 'found', event }), [])

  const toggleInterest = async (event: EventDto) => {
    replace(await (event.interested ? removeInterest(event.id) : addInterest(event.id)))
  }

  const cancel = async (event: EventDto) => {
    replace(await cancelEvent(event.id))
  }

  const comment = async (text: string) => {
    await addEventComment(eventId, text)
    reload()
  }

  const removeComment = async (id: number) => {
    await deleteEventComment(id)
    reload()
  }

  return { state, comments, toggleInterest, cancel, comment, removeComment, loadMoreComments: setCommentPage }
}
