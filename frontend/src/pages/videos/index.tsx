import { zodResolver } from '@hookform/resolvers/zod'
import { PlayIcon, PlusIcon, SearchIcon } from 'lucide-react'
import { useCallback, useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { z } from 'zod'

import { feedback } from '@/api/feedback-sink'
import { emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { AppAvatar } from '@/components/app-avatar'
import { AppButton } from '@/components/app-button'
import { AppDialog } from '@/components/app-dialog'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppField, AppForm, AppTextareaField } from '@/components/app-field'
import { AppPageHeader } from '@/components/app-page-header'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { useDebouncedValue } from '@/hooks/use-debounced-value'
import { formatRelative } from '@/lib/format'

import { getVideos, submitVideo } from './services/videos-service'
import type { VideoDto } from './services/videos-service'

const schema = z.object({
  url: z.string().trim().url('validation.required'),
  title: z.string().trim().min(3, 'validation.required'),
  description: z.string().trim(),
})

type SubmitValues = z.infer<typeof schema>

export function VideosPage() {
  const { t } = useTranslation('videos')
  const [search, setSearch] = useState('')
  const debounced = useDebouncedValue(search)
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<VideoDto>>(() => emptyPage(12))
  const [loading, setLoading] = useState(true)
  const [playing, setPlaying] = useState<VideoDto | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [open, setOpen] = useState(false)
  const [version, setVersion] = useState(0)
  const form = useForm<SubmitValues>({ resolver: zodResolver(schema), defaultValues: { url: '', title: '', description: '' } })

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    getVideos({ page, pageSize: 12, search: debounced })
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, debounced, version])

  const refresh = useCallback(() => setVersion((v) => v + 1), [])

  const submit = async (values: SubmitValues) => {
    setSubmitting(true)
    try {
      await submitVideo(values.url, values.title, values.description)
      feedback.success('', t('submit.success'))
      setOpen(false)
      form.reset()
      refresh()
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="app-page-enter flex flex-col">
      <AppPageHeader
        title={t('title')}
        description={t('description')}
        actions={
          <AppButton onClick={() => setOpen(true)}>
            <PlusIcon />
            {t('submit.action')}
          </AppButton>
        }
      />

      <div className="relative mb-6 max-w-md">
        <SearchIcon className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
        <input
          type="search"
          value={search}
          onChange={(event) => {
            setSearch(event.target.value)
            setPage(1)
          }}
          placeholder={t('searchPlaceholder')}
          aria-label={t('searchPlaceholder')}
          className="h-10 w-full rounded-lg border bg-transparent pl-9 pr-3 text-sm placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
        />
      </div>

      {loading ? (
        <AppSkeleton preset="card" count={3} className="grid gap-4 md:grid-cols-2 xl:grid-cols-3" />
      ) : result.items.length === 0 ? (
        <AppEmptyState icon={PlayIcon} title={t('empty.title')} description={t('empty.description')} />
      ) : (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {result.items.map((video) => (
            <article key={video.id} className="flex flex-col overflow-hidden rounded-xl border bg-card">
              <button
                type="button"
                onClick={() => setPlaying(video)}
                className="group relative aspect-video bg-muted focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
                aria-label={t('play', { title: video.title })}
              >
                <img src={`https://i.ytimg.com/vi/${video.youtubeId}/hqdefault.jpg`} alt="" loading="lazy" className="size-full object-cover" />
                <span className="absolute inset-0 flex items-center justify-center bg-scrim/20 transition-colors group-hover:bg-scrim/40">
                  <span className="flex size-12 items-center justify-center rounded-full bg-primary text-primary-foreground">
                    <PlayIcon className="ml-0.5 size-5 fill-current" />
                  </span>
                </span>
              </button>
              <div className="flex flex-col gap-2 p-4">
                <h2 className="line-clamp-2 font-medium leading-snug">{video.title}</h2>
                <Link to={`/perfil/${video.author.nickname}`} className="flex items-center gap-2">
                  <AppAvatar name={video.author.name} src={video.author.avatarUrl} size="sm" />
                  <span className="app-meta truncate">
                    {video.author.name} · {formatRelative(video.createdAt)}
                  </span>
                </Link>
              </div>
            </article>
          ))}
        </div>
      )}

      <AppPagination page={result.page} totalPages={result.totalPages} totalCount={result.totalCount} onChange={setPage} loading={loading} />

      <AppDialog open={playing !== null} onOpenChange={() => setPlaying(null)} title={playing?.title ?? ''} description={playing?.description} size="lg">
        {playing ? (
          <div className="aspect-video overflow-hidden rounded-lg bg-scrim">
            <iframe
              title={playing.title}
              src={`https://www.youtube-nocookie.com/embed/${playing.youtubeId}?autoplay=1`}
              allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
              allowFullScreen
              className="size-full"
            />
          </div>
        ) : null}
      </AppDialog>

      <AppDialog open={open} onOpenChange={setOpen} title={t('submit.title')} description={t('submit.description')}>
        <AppForm form={form} onSubmit={submit}>
          <AppField name="url" label={t('submit.fields.url')} type="url" placeholder="https://youtube.com/watch?v=" />
          <AppField name="title" label={t('submit.fields.title')} />
          <AppTextareaField name="description" label={t('submit.fields.description')} rows={3} />
          <AppButton type="submit" loading={submitting} className="self-end">
            {t('submit.confirm')}
          </AppButton>
        </AppForm>
      </AppDialog>
    </div>
  )
}
