const locale = 'pt-BR'

const relative = new Intl.RelativeTimeFormat(locale, { numeric: 'auto' })
const dateFormat = new Intl.DateTimeFormat(locale, { day: '2-digit', month: 'short', year: 'numeric' })
const dateTimeFormat = new Intl.DateTimeFormat(locale, { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })
const weekdayFormat = new Intl.DateTimeFormat(locale, { weekday: 'long', day: '2-digit', month: 'long' })
const timeFormat = new Intl.DateTimeFormat(locale, { hour: '2-digit', minute: '2-digit' })
const currency = new Intl.NumberFormat(locale, { style: 'currency', currency: 'BRL' })
const compact = new Intl.NumberFormat(locale, { notation: 'compact', maximumFractionDigits: 1 })

const units: [Intl.RelativeTimeFormatUnit, number][] = [
  ['year', 1000 * 60 * 60 * 24 * 365],
  ['month', 1000 * 60 * 60 * 24 * 30],
  ['week', 1000 * 60 * 60 * 24 * 7],
  ['day', 1000 * 60 * 60 * 24],
  ['hour', 1000 * 60 * 60],
  ['minute', 1000 * 60],
]

export function formatRelative(iso: string, now = Date.now()): string {
  const diff = new Date(iso).getTime() - now
  for (const [unit, ms] of units) {
    if (Math.abs(diff) >= ms) return relative.format(Math.round(diff / ms), unit)
  }
  return relative.format(0, 'second')
}

export function formatDate(iso: string): string {
  return dateFormat.format(new Date(iso))
}

export function formatDateTime(iso: string): string {
  return dateTimeFormat.format(new Date(iso))
}

export function formatWeekday(iso: string): string {
  return weekdayFormat.format(new Date(iso))
}

export function formatTime(iso: string): string {
  return timeFormat.format(new Date(iso))
}

export function formatPrice(value: number | null | undefined): string | null {
  if (value == null) return null
  return currency.format(value)
}

export function formatCompact(value: number): string {
  return compact.format(value)
}

export function dayParts(iso: string): { day: string; month: string } {
  const date = new Date(iso)
  return {
    day: date.toLocaleDateString(locale, { day: '2-digit' }),
    month: date.toLocaleDateString(locale, { month: 'short' }).replace('.', ''),
  }
}
