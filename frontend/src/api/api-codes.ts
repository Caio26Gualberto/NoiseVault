export const ApiCodes = {
  invalidCredentials: 'INVALID_CREDENTIALS',
  invalidToken: 'INVALID_TOKEN',
  accountInactive: 'ACCOUNT_INACTIVE',
  emailNotConfirmed: 'EMAIL_NOT_CONFIRMED',
  loginSuccess: 'LOGIN_SUCCESS',
  logoutSuccess: 'LOGOUT_SUCCESS',

  registrationPendingConfirmation: 'REGISTRATION_PENDING_CONFIRMATION',
  confirmationEmailSent: 'CONFIRMATION_EMAIL_SENT',
  emailConfirmed: 'EMAIL_CONFIRMED',
  emailConfirmationInvalid: 'EMAIL_CONFIRMATION_INVALID',
  passwordResetEmailSent: 'PASSWORD_RESET_EMAIL_SENT',
  passwordResetSuccess: 'PASSWORD_RESET_SUCCESS',
  passwordResetInvalid: 'PASSWORD_RESET_INVALID',
  onboardingCompleted: 'ONBOARDING_COMPLETED',

  invalidName: 'INVALID_NAME',
  invalidNickname: 'INVALID_NICKNAME',
  nicknameTaken: 'NICKNAME_TAKEN',
  invalidEmail: 'INVALID_EMAIL',
  emailAlreadyInUse: 'EMAIL_ALREADY_IN_USE',
  passwordTooWeak: 'PASSWORD_TOO_WEAK',
  registrationFailed: 'REGISTRATION_FAILED',

  passwordChanged: 'PASSWORD_CHANGED',
  passwordChangeFailed: 'PASSWORD_CHANGE_FAILED',
  passwordChangeRequired: 'PASSWORD_CHANGE_REQUIRED',
  passwordChangeNotPending: 'PASSWORD_CHANGE_NOT_PENDING',
  profileUpdated: 'PROFILE_UPDATED',
  userNotFound: 'USER_NOT_FOUND',

  tooManyRequests: 'TOO_MANY_REQUESTS',
  sessionExpired: 'SESSION_EXPIRED',
  networkError: 'NETWORK_ERROR',
  unknownError: 'UNKNOWN_ERROR',
} as const

export type ApiCode = (typeof ApiCodes)[keyof typeof ApiCodes]
