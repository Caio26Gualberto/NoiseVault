import { useCallback, useEffect, useState } from 'react'

import { ApiCodes } from '@/api/api-codes'
import { feedback } from '@/api/feedback-sink'
import { ApiError, emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { maxPageSize } from '@/components/app-pagination'
import { usePolling } from '@/hooks/use-polling'
import { MediaStatus } from '@/pages/feed/media-status'

import {
  addArchiveComment,
  attendArchive,
  deleteArchiveComment,
  getArchiveComments,
  getArchiveEntry,
  likeArchive,
  unattendArchive,
  unlikeArchive,
} from '../services/mural-service'
import type { ArchiveCommentDto, ArchiveEntryDto } from '../services/mural-service'

export type ArchiveState = { status: 'loading' } | { status: 'found'; entry: ArchiveEntryDto } | { status: 'notFound' } | { status: 'failed' }

const processingPollMs = 5000

export function useArchiveDetail(entryId: number) {
  const [state, setState] = useState<ArchiveState>({ status: 'loading' })
  const [comments, setComments] = useState<PagedResult<ArchiveCommentDto>>(() => emptyPage(maxPageSize))
  const [commentPage, setCommentPage] = useState(1)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let cancelled = false
    getArchiveEntry(entryId)
      .then((entry) => {
        if (!cancelled) setState({ status: 'found', entry })
      })
      .catch((error: unknown) => {
        if (cancelled) return
        if (error instanceof ApiError && error.code === ApiCodes.archiveEntryNotFound) {
          setState({ status: 'notFound' })
          return
        }
        if (error instanceof ApiError) feedback.error(error.code, error.backendMessage)
        setState({ status: 'failed' })
      })
    return () => {
      cancelled = true
    }
  }, [entryId, version])

  useEffect(() => {
    let cancelled = false
    getArchiveComments(entryId, { page: commentPage, pageSize: maxPageSize })
      .then((data) => {
        if (cancelled) return
        setComments((previous) => (commentPage === 1 ? data : { ...data, items: [...previous.items, ...data.items] }))
      })
      .catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [entryId, commentPage, version])

  const entry = state.status === 'found' ? state.entry : null
  const processing = entry?.media.some((item) => item.media.status === MediaStatus.processing || item.media.status === MediaStatus.uploading) ?? false

  usePolling(
    async () => {
      const fresh = await getArchiveEntry(entryId).catch(() => null)
      if (fresh) setState({ status: 'found', entry: fresh })
    },
    processing ? processingPollMs : 0,
  )

  const reload = useCallback(() => {
    setCommentPage(1)
    setVersion((value) => value + 1)
  }, [])

  const replace = (next: ArchiveEntryDto) => setState({ status: 'found', entry: next })

  return {
    state,
    comments,
    toggleLike: async (target: ArchiveEntryDto) => replace(await (target.likedByMe ? unlikeArchive(target.id) : likeArchive(target.id))),
    toggleAttendance: async (target: ArchiveEntryDto) => replace(await (target.attendedByMe ? unattendArchive(target.id) : attendArchive(target.id))),
    comment: async (text: string) => {
      await addArchiveComment(entryId, text)
      reload()
    },
    removeComment: async (id: number) => {
      await deleteArchiveComment(id)
      reload()
    },
    loadMoreComments: setCommentPage,
  }
}
