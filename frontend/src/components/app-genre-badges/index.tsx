import { AppBadge } from '@/components/app-badge'
import { useMusicGenreLabel } from '@/pages/perfil/genres'
import type { MusicGenre } from '@/pages/perfil/genres'

type AppGenreBadgesProps = {
  genres: MusicGenre[]
  max?: number
  tone?: 'neutral' | 'brand'
}

export function AppGenreBadges({ genres, max = 3, tone = 'neutral' }: AppGenreBadgesProps) {
  const label = useMusicGenreLabel()

  if (genres.length === 0) return null

  const shown = genres.slice(0, max)
  const rest = genres.length - shown.length

  return (
    <div className="flex flex-wrap gap-1.5">
      {shown.map((genre) => (
        <AppBadge key={genre} tone={tone}>
          {label(genre)}
        </AppBadge>
      ))}
      {rest > 0 ? <AppBadge tone="neutral">+{rest}</AppBadge> : null}
    </div>
  )
}
