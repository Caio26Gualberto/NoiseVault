const legacyKeys = ['access_token', 'refresh_token', 'user_email']

let accessToken: string | null = null

function forgetLegacyStorage() {
  try {
    for (const key of legacyKeys) localStorage.removeItem(key)
  } catch {
    return
  }
}

forgetLegacyStorage()

export const tokenStorage = {
  readToken(): string | null {
    return accessToken
  },

  saveToken(token: string) {
    accessToken = token
  },

  clear() {
    accessToken = null
  },
}
