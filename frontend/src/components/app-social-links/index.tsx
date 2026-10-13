import { CameraIcon, Music2Icon, VideoIcon } from 'lucide-react'

import { AppIconButton } from '@/components/app-button'

type SocialLinks = {
  instagram: string | null
  youtube: string | null
  spotify: string | null
}

type AppSocialLinksProps = {
  links: SocialLinks
}

const entries = [
  { key: 'instagram', label: 'Instagram', icon: CameraIcon },
  { key: 'youtube', label: 'YouTube', icon: VideoIcon },
  { key: 'spotify', label: 'Spotify', icon: Music2Icon },
] as const

export function AppSocialLinks({ links }: AppSocialLinksProps) {
  const present = entries.filter((entry) => links[entry.key])
  if (present.length === 0) return null

  return (
    <div className="flex gap-1">
      {present.map((entry) => {
        const Icon = entry.icon
        return (
          <AppIconButton key={entry.key} label={entry.label} intent="neutral" asChild>
            <a href={links[entry.key] ?? '#'} target="_blank" rel="noreferrer">
              <Icon />
            </a>
          </AppIconButton>
        )
      })}
    </div>
  )
}
