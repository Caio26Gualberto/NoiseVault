import type { ExternalProvider } from '@/auth/external/providers'

export type ExternalStepState = {
  provider: ExternalProvider
  ticket: string
  name: string | null
  email: string | null
  suggestedNickname: string | null
  hasPassword: boolean
  from?: string
}

export function readExternalStep(state: unknown): ExternalStepState | null {
  const step = state as Partial<ExternalStepState> | null
  return step && typeof step.ticket === 'string' && step.ticket.length > 0 && typeof step.provider === 'number'
    ? (step as ExternalStepState)
    : null
}
