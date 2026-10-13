export const Roles = {
  admin: 'Admin',
  user: 'User',
} as const

export type Role = (typeof Roles)[keyof typeof Roles]
