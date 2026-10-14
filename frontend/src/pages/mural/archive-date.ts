import type { ArchiveDate } from './services/mural-service'

const locale = 'pt-BR'
const fullDate = new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC' })
const monthYear = new Intl.DateTimeFormat(locale, { month: 'long', year: 'numeric', timeZone: 'UTC' })
const shortMonth = new Intl.DateTimeFormat(locale, { month: 'short', timeZone: 'UTC' })

function utc(date: ArchiveDate): Date {
  return new Date(Date.UTC(date.year, (date.month ?? 1) - 1, date.day ?? 1))
}

export function formatArchiveDate(date: ArchiveDate): string {
  if (date.month && date.day) return fullDate.format(utc(date))
  if (date.month) return monthYear.format(utc(date))
  return String(date.year)
}

export function archiveDateBadge(date: ArchiveDate): { top: string; bottom: string } {
  if (!date.month) return { top: String(date.year), bottom: '' }
  return { top: shortMonth.format(utc(date)).replace('.', ''), bottom: String(date.year) }
}

export function decadeOf(year: number): number {
  return year - (year % 10)
}
