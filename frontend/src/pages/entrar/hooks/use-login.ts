import { useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'

import { ApiCodes } from '@/api/api-codes'
import { ApiError } from '@/api/types'
import { resendConfirmation } from '@/auth/auth-service'
import { homePath } from '@/auth/routes'
import { useAuth } from '@/auth/use-auth'

export function useLogin() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [submitting, setSubmitting] = useState(false)
  const [resending, setResending] = useState(false)
  const [pendingEmail, setPendingEmail] = useState<string | null>(null)

  const submit = async (email: string, password: string) => {
    setSubmitting(true)
    setPendingEmail(null)
    try {
      await login(email, password)
      const from = (location.state as { from?: string } | null)?.from
      navigate(from ?? homePath, { replace: true })
    } catch (error) {
      if (error instanceof ApiError && error.code === ApiCodes.emailNotConfirmed) {
        setPendingEmail(email)
      }
    } finally {
      setSubmitting(false)
    }
  }

  const resend = async () => {
    if (!pendingEmail) return
    setResending(true)
    try {
      await resendConfirmation(pendingEmail)
    } catch {
      return
    } finally {
      setResending(false)
    }
  }

  return { submit, submitting, pendingEmail, resend, resending }
}
