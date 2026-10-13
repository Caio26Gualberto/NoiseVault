import type { LucideIcon } from 'lucide-react'
import {
  BellIcon,
  CalendarDaysIcon,
  HeadphonesIcon,
  HeartHandshakeIcon,
  ImageIcon,
  LifeBuoyIcon,
  NewspaperIcon,
  PlayIcon,
  ShieldCheckIcon,
  StoreIcon,
  UserRoundIcon,
} from 'lucide-react'
import type { ReactNode } from 'react'

import { Permissions } from '../auth/permissions'
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
import { ModeracaoPage } from '../pages/admin/moderacao'
import { BandasPage } from '../pages/bandas'
import { BemVindoPage } from '../pages/bem-vindo'
import { ConfirmarEmailPage } from '../pages/confirmar-email'
import { CriarContaPage } from '../pages/criar-conta'
import { EntrarPage } from '../pages/entrar'
import { EsqueciSenhaPage } from '../pages/esqueci-senha'
import { EventosPage } from '../pages/eventos'
import { EventoDetalhePage } from '../pages/eventos/detalhe'
import { EventoFormPage } from '../pages/eventos/form'
import { FeedPage } from '../pages/feed'
import { PostDetalhePage } from '../pages/feed/detalhe'
import { FotosPage } from '../pages/fotos'
import { LojaParceiraPage, PodcastsPage, SobrePage } from '../pages/institucional'
import { MinhaContaPage } from '../pages/minha-conta'
import { NotificacoesPage } from '../pages/notificacoes'
import { PerfilPage } from '../pages/perfil'
import { RedefinirSenhaPage } from '../pages/redefinir-senha'
import { ReportarProblemaPage } from '../pages/reportar-problema'
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
      { path: '/fotos', labelKey: 'layout:nav.photos', icon: ImageIcon, element: <FotosPage /> },
      {
        path: '/eventos',
        labelKey: 'layout:nav.events',
        icon: CalendarDaysIcon,
        element: <EventosPage />,
        mobile: true,
        subRoutes: ['/eventos/novo', '/eventos/:id/editar', '/eventos/:id'],
      },
      {
        path: '/perfil/:nickname',
        labelKey: 'layout:nav.profile',
        icon: UserRoundIcon,
        element: <PerfilPage />,
        mobile: true,
        mobilePath: (nickname) => `/perfil/${nickname}`,
      },
      { path: '/notificacoes', labelKey: 'layout:nav.notifications', icon: BellIcon, element: <NotificacoesPage /> },
    ],
  },
  {
    id: 'admin',
    labelKey: 'layout:nav.admin',
    permission: Permissions.adminAccess,
    items: [
      { path: '/admin/moderacao', labelKey: 'layout:nav.moderation', icon: ShieldCheckIcon, element: <ModeracaoPage /> },
    ],
  },
  {
    id: 'institutional',
    labelKey: 'layout:nav.institutional',
    items: [
      { path: '/reportar-problema', labelKey: 'layout:nav.report', icon: LifeBuoyIcon, element: <ReportarProblemaPage /> },
      { path: '/podcasts', labelKey: 'layout:nav.podcasts', icon: HeadphonesIcon, element: <PodcastsPage /> },
      { path: '/sobre', labelKey: 'layout:nav.about', icon: HeartHandshakeIcon, element: <SobrePage /> },
      { path: '/loja-parceira', labelKey: 'layout:nav.store', icon: StoreIcon, element: <LojaParceiraPage /> },
    ],
  },
]

export const hiddenRoutes: HiddenRoute[] = [
  { path: '/feed/:id', element: <PostDetalhePage /> },
  { path: '/eventos/novo', element: <EventoFormPage /> },
  { path: '/eventos/:id/editar', element: <EventoFormPage /> },
  { path: '/eventos/:id', element: <EventoDetalhePage /> },
  { path: '/bandas/:slug', element: <BandasPage /> },
  { path: accountPath, element: <MinhaContaPage /> },
]

export const onboardingRoute: HiddenRoute = { path: onboardingPath, element: <BemVindoPage /> }
