import { useIsPoster } from '@/hooks/use-is-poster'
import { cn } from '@/lib/utils'

type AppCoverImageProps = {
  src: string
  position?: number
  poster?: boolean
  loading?: 'lazy' | 'eager'
  className?: string
}

export function AppCoverImage({ src, position = 50, poster, loading, className }: AppCoverImageProps) {
  const detected = useIsPoster(poster === undefined ? src : null)
  const isPoster = poster ?? detected

  if (!isPoster) {
    return <img src={src} alt="" loading={loading} className={cn('size-full object-cover', className)} style={{ objectPosition: `center ${position}%` }} />
  }

  return (
    <>
      <img src={src} alt="" aria-hidden loading={loading} className={cn('absolute inset-0 size-full scale-110 object-cover blur-2xl brightness-50', className)} />
      <img src={src} alt="" loading={loading} className={cn('relative size-full object-contain', className)} />
    </>
  )
}
