import { Route, Routes } from 'react-router-dom'

import { RequireAuth } from '@/auth/require-auth'
import { EnsaioVitrinePage } from '@/pages/ensaio'
import { EnsaioMinhaFichaPage } from '@/pages/ensaio/minha-ficha'
import { EnsaioMusicoPage } from '@/pages/ensaio/musico'
import { EnsaioVagaPage } from '@/pages/ensaio/vaga'
import { EnsaioVagaFormPage } from '@/pages/ensaio/vaga-form'
import { EnsaioVagasPage } from '@/pages/ensaio/vagas'
import { NaoEncontradoPage } from '@/pages/nao-encontrado'

import { EnsaioShell } from './ensaio-shell'

const ensaioRoutes = [
  { path: 'musicos/:nickname', element: <EnsaioMusicoPage /> },
  { path: 'vagas', element: <EnsaioVagasPage /> },
  { path: 'vagas/nova', element: <EnsaioVagaFormPage /> },
  { path: 'vagas/:id', element: <EnsaioVagaPage /> },
  { path: 'vagas/:id/editar', element: <EnsaioVagaFormPage /> },
  { path: 'minha-ficha', element: <EnsaioMinhaFichaPage /> },
]

export function EnsaioRoutes() {
  return (
    <Routes>
      <Route
        element={
          <RequireAuth skipWelcome>
            <EnsaioShell />
          </RequireAuth>
        }
      >
        <Route index element={<EnsaioVitrinePage />} />
        {ensaioRoutes.map((route) => (
          <Route key={route.path} path={route.path} element={route.element} />
        ))}
        <Route path="*" element={<NaoEncontradoPage />} />
      </Route>
    </Routes>
  )
}
