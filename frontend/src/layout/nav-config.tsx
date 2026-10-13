import type { LucideIcon } from 'lucide-react'
import { CalendarDaysIcon, ImageIcon, NewspaperIcon, PlayIcon, UserRoundIcon } from 'lucide-react'
import type { ReactNode } from 'react'

import type { Permission } from '../auth/permissions'
import {
  accountPath,
  confirmEmailPath,
  forgotPasswordPath,
  homePath,
  loginPath,
  onboardingPath,
  registerPath,
  resetPasswordPath,
} from '../auth/routes'
import { BemVindoPage } from '../pages/bem-vindo'
import { ConfirmarEmailPage } from '../pages/confirmar-email'
import { CriarContaPage } from '../pages/criar-conta'
import { EntrarPage } from '../pages/entrar'
import { EsqueciSenhaPage } from '../pages/esqueci-senha'
import { EventosPage } from '../pages/eventos'
import { FeedPage } from '../pages/feed'
import { FotosPage } from '../pages/fotos'
import { MinhaContaPage } from '../pages/minha-conta'
import { PerfilPage } from '../pages/perfil'
import { RedefinirSenhaPage } from '../pages/redefinir-senha'
import { VideosPage } from '../pages/videos'

export type PublicRoute = {
  path: string
  element: ReactNode
  guestOnly?: boolean
}

export type NavLeaf = {
  path: string
  labelKey: string
  icon: LucideIcon
  element: ReactNode
  permission?: Permission
  subRoutes?: string[]
  mobile?: boolean
  mobilePath?: (nickname: string) => string
}

export type NavSection = {
  id: string
  labelKey: string
  permission?: Permission
  items: NavLeaf[]
}

export type HiddenRoute = {
  path: string
  element: ReactNode
  permission?: Permission
}

export const publicRoutes: PublicRoute[] = [
  { path: loginPath, element: <EntrarPage />, guestOnly: true },
  { path: registerPath, element: <CriarContaPage />, guestOnly: true },
  { path: forgotPasswordPath, element: <EsqueciSenhaPage />, guestOnly: true },
  { path: confirmEmailPath, element: <ConfirmarEmailPage /> },
  { path: resetPasswordPath, element: <RedefinirSenhaPage /> },
]

export const navSections: NavSection[] = [
  {
    id: 'community',
    labelKey: 'layout:nav.community',
    items: [
      { path: homePath, labelKey: 'layout:nav.feed', icon: NewspaperIcon, element: <FeedPage />, mobile: true, subRoutes: ['/feed/:id'] },
      { path: '/videos', labelKey: 'layout:nav.videos', icon: PlayIcon, element: <VideosPage />, mobile: true },
      { path: '/fotos', labelKey: 'layout:nav.photos', icon: ImageIcon, element: <FotosPage />, mobile: true },
      {
        path: '/eventos',
        labelKey: 'layout:nav.events',
        icon: CalendarDaysIcon,
        element: <EventosPage />,
        mobile: true,
        subRoutes: ['/eventos/:id', '/eventos/novo', '/eventos/:id/editar'],
      },
      {
        path: '/perfil/:nickname',
        labelKey: 'layout:nav.profile',
        icon: UserRoundIcon,
        element: <PerfilPage />,
        mobile: true,
        mobilePath: (nickname) => `/perfil/${nickname}`,
      },
    ],
  },
]

export const hiddenRoutes: HiddenRoute[] = [
  { path: accountPath, element: <MinhaContaPage /> },
]

export const onboardingRoute: HiddenRoute = { path: onboardingPath, element: <BemVindoPage /> }
