import type { LucideIcon } from 'lucide-react'
import {
  ActivityIcon,
  HeadsetIcon,
  CalendarDaysIcon,
  ArchiveIcon,
  BadgeCheckIcon,
  CassetteTapeIcon,
  HeadphonesIcon,
  LifeBuoyIcon,
  NewspaperIcon,
  RssIcon,
  StoreIcon,
  UserRoundIcon,
} from 'lucide-react'
import type { ReactNode } from 'react'

import { Permissions } from '../auth/permissions'
import type { Permission } from '../auth/permissions'
import {
  accountPath,
  profileEditPath,
  confirmEmailPath,
  dataDeletionPath,
  externalLinkPath,
  externalSignupPath,
  forgotPasswordPath,
  homePath,
  loginPath,
  privacyPath,
  registerPath,
  resetPasswordPath,
  welcomePath,
} from '../auth/routes'
import { BandasPage } from '../pages/bandas'
import { BandaEditarPage } from '../pages/bandas/editar'
import { BandaNovaPage } from '../pages/bandas/nova'
import { BandKind } from '../pages/bandas/band-kind'
import { BemVindoPage } from '../pages/bem-vindo'
import { ConfirmarEmailPage } from '../pages/confirmar-email'
import { CriarContaPage } from '../pages/criar-conta'
import { EntrarPage } from '../pages/entrar'
import { EscolherApelidoPage } from '../pages/entrar/apelido'
import { VincularContaPage } from '../pages/entrar/vincular'
import { EsqueciSenhaPage } from '../pages/esqueci-senha'
import { EventosPage } from '../pages/eventos'
import { EventoDetalhePage } from '../pages/eventos/detalhe'
import { EventoFormPage } from '../pages/eventos/form'
import { FeedPage } from '../pages/feed'
import { PostDetalhePage } from '../pages/feed/detalhe'
import { PodcastsPage } from '../pages/institucional'
import { ApagarDadosPage } from '../pages/institucional/apagar-dados'
import { LojaParceiraPage } from '../pages/institucional/loja'
import { PrivacidadePage } from '../pages/institucional/privacidade'
import { SobrePage } from '../pages/institucional/sobre'
import { NoticiasPage } from '../pages/noticias'
import { MinhaContaPage } from '../pages/minha-conta'
import { MeuPerfilPage } from '../pages/meu-perfil'
import { PerfilPage } from '../pages/perfil'
import { RedefinirSenhaPage } from '../pages/redefinir-senha'
import { AjudaPage } from '../pages/ajuda'
import { helpPath } from './last-location'
import { SuporteAdminPage } from '../pages/admin/suporte'
import { SuporteAdminDetalhePage } from '../pages/admin/suporte/detalhe'
import { SuportePage } from '../pages/suporte'
import { SuporteDetalhePage } from '../pages/suporte/detalhe'
import { MuralPage } from '../pages/mural'
import { MuralDetalhePage } from '../pages/mural/detalhe'
import { MuralFormPage } from '../pages/mural/form'
import { MuralEnviosPage } from '../pages/mural/envios'
import { BandasAdminPage } from '../pages/admin/bandas'
import { adminBandsPath } from '../pages/bandas/band-verification-status'
import { JobsAdminPage } from '../pages/admin/jobs'
import { MuralAdminPage } from '../pages/admin/mural'
import { adminArchivePath, mySubmissionsPath, submitArchivePath } from '../pages/mural/archive-status'

export type PublicRoute = {
  path: string
  element: ReactNode
  guestOnly?: boolean
  wide?: boolean
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
  public?: boolean
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
  public?: boolean
}

export const publicRoutes: PublicRoute[] = [
  { path: loginPath, element: <EntrarPage />, guestOnly: true },
  { path: externalSignupPath, element: <EscolherApelidoPage />, guestOnly: true },
  { path: externalLinkPath, element: <VincularContaPage />, guestOnly: true },
  { path: registerPath, element: <CriarContaPage />, guestOnly: true },
  { path: forgotPasswordPath, element: <EsqueciSenhaPage />, guestOnly: true },
  { path: confirmEmailPath, element: <ConfirmarEmailPage /> },
  { path: resetPasswordPath, element: <RedefinirSenhaPage /> },
  { path: privacyPath, element: <PrivacidadePage />, wide: true },
  { path: dataDeletionPath, element: <ApagarDadosPage />, wide: true },
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
        public: true,
        subRoutes: ['/mural/novo', submitArchivePath, mySubmissionsPath, '/mural/:id/editar', '/mural/:id'],
      },
      {
        path: '/eventos',
        labelKey: 'layout:nav.events',
        icon: CalendarDaysIcon,
        element: <EventosPage />,
        mobile: true,
        public: true,
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
      { path: '/noticias', labelKey: 'layout:nav.news', icon: RssIcon, element: <NoticiasPage />, mobile: true, public: true },
    ],
  },
  {
    id: 'institutional',
    labelKey: 'layout:nav.institutional',
    items: [
      {
        path: helpPath,
        labelKey: 'layout:nav.help',
        icon: LifeBuoyIcon,
        element: <AjudaPage />,
        subRoutes: ['/suporte', '/suporte/:id'],
      },
      { path: '/podcasts', labelKey: 'layout:nav.podcasts', icon: HeadphonesIcon, element: <PodcastsPage /> },
      { path: '/loja-parceira', labelKey: 'layout:nav.store', icon: StoreIcon, element: <LojaParceiraPage /> },
    ],
  },
  {
    id: 'admin',
    labelKey: 'layout:nav.admin',
    items: [
      {
        path: adminArchivePath,
        labelKey: 'layout:nav.archiveInbox',
        icon: ArchiveIcon,
        element: <MuralAdminPage />,
        permission: Permissions.archiveReview,
      },
      {
        path: adminBandsPath,
        labelKey: 'layout:nav.bandVerifications',
        icon: BadgeCheckIcon,
        element: <BandasAdminPage />,
        permission: Permissions.bandsVerificationsView,
      },
      {
        path: '/admin/suporte',
        labelKey: 'layout:nav.supportInbox',
        icon: HeadsetIcon,
        element: <SuporteAdminPage />,
        permission: Permissions.supportView,
        subRoutes: ['/admin/suporte/:id'],
      },
      {
        path: '/admin/jobs',
        labelKey: 'layout:nav.jobs',
        icon: ActivityIcon,
        element: <JobsAdminPage />,
        permission: Permissions.systemJobsView,
      },
    ],
  },
]

export const hiddenRoutes: HiddenRoute[] = [
  { path: '/feed/:id', element: <PostDetalhePage /> },
  { path: '/sobre', element: <SobrePage /> },
  { path: '/mural/novo', element: <MuralFormPage />, permission: Permissions.archiveCreate },
  { path: submitArchivePath, element: <MuralFormPage mode="submission" /> },
  { path: mySubmissionsPath, element: <MuralEnviosPage /> },
  { path: '/mural/:id/editar', element: <MuralFormPage /> },
  { path: '/mural/:id', element: <MuralDetalhePage />, public: true },
  { path: '/eventos/novo', element: <EventoFormPage /> },
  { path: '/eventos/:id/editar', element: <EventoFormPage /> },
  { path: '/eventos/:id', element: <EventoDetalhePage />, public: true },
  { path: '/bandas/nova', element: <BandaNovaPage kind={BandKind.band} /> },
  { path: '/bandas/:slug/editar', element: <BandaEditarPage /> },
  { path: '/bandas/:slug', element: <BandasPage />, public: true },
  { path: '/artistas/nova', element: <BandaNovaPage kind={BandKind.solo} /> },
  { path: '/artistas/:slug/editar', element: <BandaEditarPage /> },
  { path: '/artistas/:slug', element: <BandasPage />, public: true },
  { path: '/suporte', element: <SuportePage /> },
  { path: '/suporte/:id', element: <SuporteDetalhePage /> },
  { path: '/admin/suporte/:id', element: <SuporteAdminDetalhePage />, permission: Permissions.supportView },
  { path: accountPath, element: <MinhaContaPage /> },
  { path: profileEditPath, element: <MeuPerfilPage /> },
]

export const welcomeRoute: HiddenRoute = { path: welcomePath, element: <BemVindoPage /> }
