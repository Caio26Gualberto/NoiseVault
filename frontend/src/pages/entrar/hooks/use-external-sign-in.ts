import { useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'

import { ExternalLoginOutcome } from '@/api/types'
import { externalLogin } from '@/auth/auth-service'
import type { ExternalProvider } from '@/auth/external/providers'
import { externalLinkPath, externalSignupPath } from '@/auth/routes'
import { useAuth } from '@/auth/use-auth'

import type { ExternalStepState } from './external-step'
import { useFinishSignIn } from './use-finish-sign-in'

export function useExternalSignIn() {
  const { acceptToken } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const finish = useFinishSignIn()
  const [busy, setBusy] = useState<ExternalProvider | null>(null)

  const signIn = async (provider: ExternalProvider, token: string) => {
    setBusy(provider)
    try {
      const result = await externalLogin(provider, token)

      if (result.outcome === ExternalLoginOutcome.signedIn && result.tokens) {
        await acceptToken(result.tokens.token)
        finish()
        return
      }

      if (!result.ticket) return

      const step: ExternalStepState = {
        provider,
        ticket: result.ticket,
        name: result.name,
        email: result.email,
        suggestedNickname: result.suggestedNickname,
        hasPassword: result.hasPassword,
        from: (location.state as { from?: string } | null)?.from,
      }

      navigate(
        {
          pathname: result.outcome === ExternalLoginOutcome.signupRequired ? externalSignupPath : externalLinkPath,
          search: location.search,
        },
        { state: step },
      )
    } catch {
      return
    } finally {
      setBusy(null)
    }
  }

  return { signIn, busy }
}
