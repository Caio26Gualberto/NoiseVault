import { z } from 'zod'

export const nameSchema = z
  .string()
  .trim()
  .min(2, 'validation.nameLength')
  .max(100, 'validation.nameLength')

export const nicknameSchema = z
  .string()
  .trim()
  .toLowerCase()
  .regex(/^[a-z0-9](?:[a-z0-9._]{1,28}[a-z0-9])?$/, 'validation.nickname')

export const emailSchema = z.string().trim().toLowerCase().email('validation.email')

export const passwordSchema = z
  .string()
  .min(6, 'validation.password')
  .regex(/\d/, 'validation.password')

type PasswordPair = { password: string; confirmPassword: string }

export function withPasswordConfirmation<S extends z.ZodObject<z.ZodRawShape>>(schema: S) {
  return schema.refine((value) => (value as PasswordPair).password === (value as PasswordPair).confirmPassword, {
    message: 'validation.passwordMatch',
    path: ['confirmPassword'],
  })
}
