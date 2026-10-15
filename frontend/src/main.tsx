import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'

import './theme/global.css'
import './i18n'

import { App } from './app'
import { applyEnsaioFrame } from './ensaio/boot'
import { AuthProvider } from './auth/auth-provider'
import { AppConfirmProvider } from './components/app-confirm'
import { Toaster } from './components/ui/sonner'
import { TooltipProvider } from './components/ui/tooltip'
import { listenInstallPrompt } from './pwa/install-prompt'
import { registerServiceWorker } from './pwa/register-service-worker'
import { applyColorScheme, readColorScheme } from './theme/color-scheme'
import { registerToastFeedback } from './toast-feedback'

registerToastFeedback()
listenInstallPrompt()
registerServiceWorker()
applyColorScheme(readColorScheme())
applyEnsaioFrame()

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
      <Toaster
        position="bottom-right"
        richColors
        closeButton
        offset={{ bottom: 'var(--toast-offset-bottom)', right: 24 }}
        mobileOffset={{ bottom: 'var(--toast-offset-bottom)', right: 16, left: 16 }}
      />
    </TooltipProvider>
  </StrictMode>,
)
