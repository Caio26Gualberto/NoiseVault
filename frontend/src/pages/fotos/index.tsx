import { ImageIcon, UploadIcon } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { AppAvatar } from '@/components/app-avatar'
import { AppButton } from '@/components/app-button'
import { AppDialog } from '@/components/app-dialog'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppPageHeader } from '@/components/app-page-header'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { formatDate } from '@/lib/format'

import { getPhotos } from './services/fotos-service'
import type { PhotoDto } from './services/fotos-service'

export function FotosPage() {
  const { t } = useTranslation('fotos')
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<PhotoDto>>(() => emptyPage(12))
  const [loading, setLoading] = useState(true)
  const [open, setOpen] = useState<PhotoDto | null>(null)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    getPhotos({ page, pageSize: 12 })
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page])

  return (
    <div className="app-page-enter flex flex-col">
      <AppPageHeader
        title={t('title')}
        description={t('description')}
        actions={
          <Tooltip>
            <TooltipTrigger asChild>
              <span>
                <AppButton disabled>
                  <UploadIcon />
                  {t('upload')}
                </AppButton>
              </span>
            </TooltipTrigger>
            <TooltipContent>{t('uploadSoon')}</TooltipContent>
          </Tooltip>
        }
      />

      {loading ? (
        <div className="grid grid-cols-2 gap-2 md:grid-cols-3 md:gap-3">
          {Array.from({ length: 6 }, (_, index) => (
            <AppSkeleton key={index} preset="lines" count={1} className="aspect-square [&>*]:h-full" />
          ))}
        </div>
      ) : result.items.length === 0 ? (
        <AppEmptyState icon={ImageIcon} title={t('empty.title')} description={t('empty.description')} />
      ) : (
        <div className="grid grid-cols-2 gap-2 md:grid-cols-3 md:gap-3">
          {result.items.map((photo) => (
            <button
              key={photo.id}
              type="button"
              onClick={() => setOpen(photo)}
              className="group relative aspect-square overflow-hidden rounded-lg bg-muted focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
            >
              <img src={photo.imageUrl} alt={photo.title} loading="lazy" className="size-full object-cover transition-transform duration-300 group-hover:scale-[1.03]" />
              <span className="absolute inset-x-0 bottom-0 flex flex-col bg-gradient-to-t from-scrim/80 to-transparent p-3 text-left text-on-scrim md:translate-y-2 md:opacity-0 md:transition-all md:group-hover:translate-y-0 md:group-hover:opacity-100 md:group-focus-visible:translate-y-0 md:group-focus-visible:opacity-100">
                <span className="truncate text-sm font-medium">{photo.title}</span>
                {photo.subtitle ? <span className="truncate text-xs text-on-scrim/80">{photo.subtitle}</span> : null}
              </span>
            </button>
          ))}
        </div>
      )}

      <AppPagination page={result.page} totalPages={result.totalPages} totalCount={result.totalCount} onChange={setPage} loading={loading} />

      <AppDialog open={open !== null} onOpenChange={() => setOpen(null)} title={open?.title ?? ''} description={open?.subtitle ?? undefined} size="lg">
        {open ? (
          <div className="flex flex-col gap-3">
            <img src={open.imageUrl} alt={open.title} className="max-h-[65vh] w-full rounded-lg object-contain" />
            <div className="flex items-center justify-between gap-3">
              <Link to={`/perfil/${open.author.nickname}`} className="flex items-center gap-2">
                <AppAvatar name={open.author.name} src={open.author.avatarUrl} size="sm" />
                <span className="text-sm">{open.author.name}</span>
              </Link>
              <span className="app-meta">{formatDate(open.captureDate)}</span>
            </div>
          </div>
        ) : null}
      </AppDialog>
    </div>
  )
}
