const ACCESS_TOKEN_KEY = 'access_token'
const REFRESH_TOKEN_KEY = 'refresh_token'
const USER_EMAIL_KEY = 'user_email'

export const tokenStorage = {
  readToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY)
  },

  readRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY)
  },

  readEmail(): string | null {
    return localStorage.getItem(USER_EMAIL_KEY)
  },

  saveTokens(token: string, refreshToken: string) {
    localStorage.setItem(ACCESS_TOKEN_KEY, token)
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken)
  },

  saveEmail(email: string) {
    localStorage.setItem(USER_EMAIL_KEY, email)
  },

  clear() {
    localStorage.removeItem(ACCESS_TOKEN_KEY)
    localStorage.removeItem(REFRESH_TOKEN_KEY)
    localStorage.removeItem(USER_EMAIL_KEY)
  },
}
