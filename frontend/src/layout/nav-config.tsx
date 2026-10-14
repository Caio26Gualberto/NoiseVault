import type { LucideIcon } from 'lucide-react'
import {
  BellIcon,
  HeadsetIcon,
  CalendarDaysIcon,
  CassetteTapeIcon,
  HeadphonesIcon,
  HeartHandshakeIcon,
  LifeBuoyIcon,
  NewspaperIcon,
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
  registerPath,
  resetPasswordPath,
  welcomePath,
} from '../auth/routes'
import { BandasPage } from '../pages/bandas'
import { BandaEditarPage } from '../pages/bandas/editar'
import { BandaNovaPage } from '../pages/bandas/nova'
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
import { LojaParceiraPage, PodcastsPage } from '../pages/institucional'
import { SobrePage } from '../pages/institucional/sobre'
import { MinhaContaPage } from '../pages/minha-conta'
import { NotificacoesPage } from '../pages/notificacoes'
import { PerfilPage } from '../pages/perfil'
import { RedefinirSenhaPage } from '../pages/redefinir-senha'
import { ReportarProblemaPage } from '../pages/reportar-problema'
import { SuporteAdminPage } from '../pages/admin/suporte'
import { SuporteAdminDetalhePage } from '../pages/admin/suporte/detalhe'
import { SuportePage } from '../pages/suporte'
import { SuporteDetalhePage } from '../pages/suporte/detalhe'
import { MuralPage } from '../pages/mural'
import { MuralDetalhePage } from '../pages/mural/detalhe'
import { MuralFormPage } from '../pages/mural/form'

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
      {
        path: '/mural',
        labelKey: 'layout:nav.archive',
        icon: CassetteTapeIcon,
        element: <MuralPage />,
        mobile: true,
        subRoutes: ['/mural/novo', '/mural/:id/editar', '/mural/:id'],
      },
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
    id: 'institutional',
    labelKey: 'layout:nav.institutional',
    items: [
      {
        path: '/reportar-problema',
        labelKey: 'layout:nav.report',
        icon: LifeBuoyIcon,
        element: <ReportarProblemaPage />,
        subRoutes: ['/suporte', '/suporte/:id'],
      },
      { path: '/podcasts', labelKey: 'layout:nav.podcasts', icon: HeadphonesIcon, element: <PodcastsPage /> },
      { path: '/sobre', labelKey: 'layout:nav.about', icon: HeartHandshakeIcon, element: <SobrePage /> },
      { path: '/loja-parceira', labelKey: 'layout:nav.store', icon: StoreIcon, element: <LojaParceiraPage /> },
    ],
  },
  {
    id: 'admin',
    labelKey: 'layout:nav.admin',
    permission: Permissions.supportView,
    items: [
      {
        path: '/admin/suporte',
        labelKey: 'layout:nav.supportInbox',
        icon: HeadsetIcon,
        element: <SuporteAdminPage />,
        permission: Permissions.supportView,
        subRoutes: ['/admin/suporte/:id'],
      },
    ],
  },
]

export const hiddenRoutes: HiddenRoute[] = [
  { path: '/feed/:id', element: <PostDetalhePage /> },
  { path: '/mural/novo', element: <MuralFormPage />, permission: Permissions.archiveCreate },
  { path: '/mural/:id/editar', element: <MuralFormPage />, permission: Permissions.archiveEdit },
  { path: '/mural/:id', element: <MuralDetalhePage /> },
  { path: '/eventos/novo', element: <EventoFormPage /> },
  { path: '/eventos/:id/editar', element: <EventoFormPage /> },
  { path: '/eventos/:id', element: <EventoDetalhePage /> },
  { path: '/bandas/nova', element: <BandaNovaPage /> },
  { path: '/bandas/:slug/editar', element: <BandaEditarPage /> },
  { path: '/bandas/:slug', element: <BandasPage /> },
  { path: '/suporte', element: <SuportePage /> },
  { path: '/suporte/:id', element: <SuporteDetalhePage /> },
  { path: '/admin/suporte/:id', element: <SuporteAdminDetalhePage />, permission: Permissions.supportView },
  { path: accountPath, element: <MinhaContaPage /> },
]

export const welcomeRoute: HiddenRoute = { path: welcomePath, element: <BemVindoPage /> }
