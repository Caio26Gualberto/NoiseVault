import { Navigate, Route, Routes } from 'react-router-dom'

import { RedirectIfAuthenticated } from './auth/redirect-if-authenticated'
import { RequireAuth } from './auth/require-auth'
import { homePath, initialPasswordPath } from './auth/routes'
import { isEnsaioHost } from './ensaio/frame'
import { AppShell } from './layout/app-shell'
import { EnsaioRoutes } from './layout/ensaio/ensaio-routes'
import { hiddenRoutes, navSections, publicRoutes, welcomeRoute } from './layout/nav-config'
import { PublicLayout } from './layout/public-layout'
import { DefinirSenhaPage } from './pages/definir-senha'
import { NaoEncontradoPage } from './pages/nao-encontrado'

const shellRoutes = [
  ...navSections.flatMap((section) =>
    section.items.map((item) => ({
      path: item.path,
      permission: item.permission ?? section.permission,
      element: item.element,
    })),
  ),
  ...hiddenRoutes,
]

export function App() {
  if (isEnsaioHost) return <EnsaioRoutes />

  return (
    <Routes>
      <Route path="/ensaio/*" element={<EnsaioRoutes />} />

      <Route element={<PublicLayout />}>
        {publicRoutes
          .filter((route) => !route.wide)
          .map((route) => (
            <Route
              key={route.path}
              path={route.path}
              element={route.guestOnly ? <RedirectIfAuthenticated>{route.element}</RedirectIfAuthenticated> : route.element}
            />
          ))}
      </Route>

      <Route element={<PublicLayout wide />}>
        {publicRoutes
          .filter((route) => route.wide)
          .map((route) => (
            <Route key={route.path} path={route.path} element={route.element} />
          ))}
      </Route>

      <Route
        path={initialPasswordPath}
        element={
          <RequireAuth>
            <DefinirSenhaPage />
          </RequireAuth>
        }
      />
      <Route path={welcomeRoute.path} element={<RequireAuth>{welcomeRoute.element}</RequireAuth>} />

      <Route
        element={
          <RequireAuth>
            <AppShell />
          </RequireAuth>
        }
      >
        <Route index element={<Navigate to={homePath} replace />} />
        {shellRoutes.map((route) => (
          <Route
            key={route.path}
            path={route.path}
            element={<RequireAuth permission={route.permission}>{route.element}</RequireAuth>}
          />
        ))}
        <Route path="*" element={<NaoEncontradoPage />} />
      </Route>
    </Routes>
  )
}
