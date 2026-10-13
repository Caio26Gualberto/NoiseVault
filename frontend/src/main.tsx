import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'

import './theme/global.css'
import './i18n'

import { App } from './app'
import { AuthProvider } from './auth/auth-provider'
import { AppConfirmProvider } from './components/app-confirm'
import { Toaster } from './components/ui/sonner'
import { TooltipProvider } from './components/ui/tooltip'
import { applyColorScheme, readColorScheme } from './theme/color-scheme'
import { registerToastFeedback } from './toast-feedback'

registerToastFeedback()
applyColorScheme(readColorScheme())

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <TooltipProvider delayDuration={300}>
      <BrowserRouter>
        <AuthProvider>
          <AppConfirmProvider>
            <App />
          </AppConfirmProvider>
        </AuthProvider>
      </BrowserRouter>
      <Toaster position="top-center" richColors closeButton />
    </TooltipProvider>
  </StrictMode>,
)
