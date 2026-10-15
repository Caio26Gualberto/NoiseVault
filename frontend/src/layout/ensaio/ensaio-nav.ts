import { IdCardIcon, MegaphoneIcon, UsersIcon } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'

export type EnsaioNavItem = {
  path: string
  labelKey: string
  icon: LucideIcon
  matches: (path: string) => boolean
}

export const ensaioNav: EnsaioNavItem[] = [
  { path: '/', labelKey: 'nav.musicians', icon: UsersIcon, matches: (path) => path === '/' || path.startsWith('/musicos') },
  { path: '/vagas', labelKey: 'nav.openings', icon: MegaphoneIcon, matches: (path) => path.startsWith('/vagas') },
  { path: '/minha-ficha', labelKey: 'nav.myProfile', icon: IdCardIcon, matches: (path) => path.startsWith('/minha-ficha') },
]
