import { toast } from 'sonner'

import { setFeedbackSink } from './api/feedback-sink'
import { apiMessage } from './i18n'

export function registerToastFeedback() {
  setFeedbackSink({
    error: (code, backendMessage) => {
      toast.error(apiMessage(code, backendMessage), { duration: 5000 })
    },
    success: (code, backendMessage) => {
      toast.success(apiMessage(code, backendMessage), { duration: 3500 })
    },
  })
}
