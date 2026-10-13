import { useCallback, useEffect, useState } from 'react'

import { ApiCodes } from '@/api/api-codes'
import { feedback } from '@/api/feedback-sink'
import { ApiError, emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { maxPageSize } from '@/components/app-pagination'
import { usePolling } from '@/hooks/use-polling'

import { PostStatus } from '../post-status'
import { addComment, deleteComment, getComments, getPost, likePost, unlikePost } from '../services/feed-service'
import type { CommentDto, PostDto } from '../services/feed-service'

export type PostState = { status: 'loading' } | { status: 'found'; post: PostDto } | { status: 'notFound' } | { status: 'failed' }

const processingPollMs = 5000

export function usePostDetail(postId: number) {
  const [state, setState] = useState<PostState>({ status: 'loading' })
  const [comments, setComments] = useState<PagedResult<CommentDto>>(() => emptyPage(maxPageSize))
  const [commentPage, setCommentPage] = useState(1)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let cancelled = false
    getPost(postId)
      .then((post) => {
        if (!cancelled) setState({ status: 'found', post })
      })
      .catch((error: unknown) => {
        if (cancelled) return
        if (error instanceof ApiError && error.code === ApiCodes.postNotFound) {
          setState({ status: 'notFound' })
          return
        }
        if (error instanceof ApiError) feedback.error(error.code, error.backendMessage)
        setState({ status: 'failed' })
      })
    return () => {
      cancelled = true
    }
  }, [postId, version])

  useEffect(() => {
    let cancelled = false
    getComments(postId, { page: commentPage, pageSize: maxPageSize })
      .then((data) => {
        if (cancelled) return
        setComments((previous) => (commentPage === 1 ? data : { ...data, items: [...previous.items, ...data.items] }))
      })
      .catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [postId, commentPage, version])

  const reload = useCallback(() => {
    setCommentPage(1)
    setVersion((value) => value + 1)
  }, [])

  const post = state.status === 'found' ? state.post : null

  usePolling(
    async () => {
      if (!post) return
      const fresh = await getPost(post.id).catch(() => null)
      if (fresh && fresh.status !== post.status) setState({ status: 'found', post: fresh })
    },
    post && post.status === PostStatus.processing && post.canEdit ? processingPollMs : 0,
  )

  const replace = useCallback((next: PostDto) => setState({ status: 'found', post: next }), [])

  const like = async (target: PostDto) => {
    replace(target.likedByMe ? await unlikePost(target.id) : await likePost(target.id))
  }

  const comment = async (text: string) => {
    await addComment(postId, text)
    reload()
  }

  const removeComment = async (id: number) => {
    await deleteComment(id)
    reload()
  }

  return { state, comments, like, replace, comment, removeComment, loadMoreComments: setCommentPage }
}
