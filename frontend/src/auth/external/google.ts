import { loadScript } from './load-script'
import { googleClientId } from './providers'

type GoogleCredentialResponse = { credential?: string }

type GoogleButtonOptions = {
  type?: 'standard' | 'icon'
  theme?: 'outline' | 'filled_blue' | 'filled_black'
  size?: 'large' | 'medium' | 'small'
  text?: 'signin_with' | 'signup_with' | 'continue_with' | 'signin'
  shape?: 'rectangular' | 'pill' | 'circle' | 'square'
  logo_alignment?: 'left' | 'center'
  width?: number
  locale?: string
}

type GoogleIdentity = {
  initialize: (options: {
    client_id: string
    callback: (response: GoogleCredentialResponse) => void
    ux_mode?: 'popup' | 'redirect'
    auto_select?: boolean
    cancel_on_tap_outside?: boolean
    use_fedcm_for_button?: boolean
  }) => void
  renderButton: (parent: HTMLElement, options: GoogleButtonOptions) => void
  disableAutoSelect: () => void
}

declare global {
  interface Window {
    google?: { accounts: { id: GoogleIdentity } }
  }
}

const sdkUrl = 'https://accounts.google.com/gsi/client'

let onCredential: ((token: string) => void) | null = null
let initialized: Promise<GoogleIdentity> | null = null

function initialize(): Promise<GoogleIdentity> {
  initialized ??= loadScript(sdkUrl)
    .then(() => {
      const identity = window.google?.accounts.id
      if (!identity) throw new Error('google')
      identity.initialize({
        client_id: googleClientId,
        callback: (response) => {
          if (response.credential) onCredential?.(response.credential)
        },
        ux_mode: 'popup',
        auto_select: false,
        cancel_on_tap_outside: true,
        use_fedcm_for_button: true,
      })
      return identity
    })
    .catch((error: unknown) => {
      initialized = null
      throw error
    })
  return initialized
}

export async function renderGoogleButton(
  parent: HTMLElement,
  options: GoogleButtonOptions,
  handleCredential: (token: string) => void,
) {
  const identity = await initialize()
  onCredential = handleCredential
  parent.replaceChildren()
  identity.renderButton(parent, options)
}
