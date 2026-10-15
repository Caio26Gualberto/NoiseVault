import { CalendarIcon } from 'lucide-react'
import { useState } from 'react'
import type { ChangeEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { ptBR } from 'react-day-picker/locale'

import { AppButton } from '@/components/app-button'
import { Calendar } from '@/components/ui/calendar'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { cn } from '@/lib/utils'

export const invalidDateTime = 'invalid'
const defaultTime = '20:00'

const pad = (value: number) => String(value).padStart(2, '0')

function maskDateTime(raw: string) {
  const digits = raw.replace(/\D/g, '').slice(0, 12)
  const parts = [digits.slice(0, 2), digits.slice(2, 4), digits.slice(4, 8), digits.slice(8, 10), digits.slice(10, 12)]
  let out = parts[0]
  if (digits.length > 2) out += `/${parts[1]}`
  if (digits.length > 4) out += `/${parts[2]}`
  if (digits.length > 8) out += ` ${parts[3]}`
  if (digits.length > 10) out += `:${parts[4]}`
  return out
}

function maskTime(raw: string) {
  const digits = raw.replace(/\D/g, '').slice(0, 4)
  return digits.length > 2 ? `${digits.slice(0, 2)}:${digits.slice(2)}` : digits
}

function isValidTime(time: string) {
  const match = /^(\d{2}):(\d{2})$/.exec(time)
  return match !== null && Number(match[1]) < 24 && Number(match[2]) < 60
}

function toValue(text: string) {
  const digits = text.replace(/\D/g, '')
  if (digits.length === 0) return ''
  if (digits.length < 12) return invalidDateTime
  const day = Number(digits.slice(0, 2))
  const month = Number(digits.slice(2, 4))
  const year = Number(digits.slice(4, 8))
  const time = `${digits.slice(8, 10)}:${digits.slice(10, 12)}`
  const date = new Date(year, month - 1, day)
  const exists = date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day
  if (!exists || !isValidTime(time) || year < 1900) return invalidDateTime
  return `${year}-${pad(month)}-${pad(day)}T${time}`
}

function toText(value: string) {
  const match = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})$/.exec(value)
  return match ? `${match[3]}/${match[2]}/${match[1]} ${match[4]}:${match[5]}` : ''
}

function toDate(value: string) {
  const match = /^(\d{4})-(\d{2})-(\d{2})T/.exec(value)
  return match ? new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3])) : undefined
}

type AppDateTimeInputProps = {
  value: string
  onChange: (value: string) => void
  onBlur?: () => void
  id?: string
  invalid?: boolean
  describedBy?: string
}

export function AppDateTimeInput({ value, onChange, onBlur, id, invalid, describedBy }: AppDateTimeInputProps) {
  const { t } = useTranslation()
  const [text, setText] = useState(() => toText(value))
  const [shown, setShown] = useState(value)
  const [open, setOpen] = useState(false)
  const [draftDay, setDraftDay] = useState<Date | undefined>(undefined)
  const [draftTime, setDraftTime] = useState(defaultTime)

  if (value !== shown) {
    setShown(value)
    if (value !== invalidDateTime && value !== toValue(text)) setText(toText(value))
  }

  const type = (event: ChangeEvent<HTMLInputElement>) => {
    const masked = maskDateTime(event.target.value)
    const next = toValue(masked)
    setText(masked)
    setShown(next)
    onChange(next)
  }

  const changeOpen = (next: boolean) => {
    setOpen(next)
    if (!next) return
    setDraftDay(toDate(value))
    setDraftTime(/T(\d{2}:\d{2})$/.exec(value)?.[1] ?? defaultTime)
  }

  const apply = () => {
    if (!draftDay || !isValidTime(draftTime)) return
    const next = `${draftDay.getFullYear()}-${pad(draftDay.getMonth() + 1)}-${pad(draftDay.getDate())}T${draftTime}`
    setText(toText(next))
    setShown(next)
    onChange(next)
    setOpen(false)
  }

  return (
    <div className="relative">
      <input
        id={id}
        value={text}
        onChange={type}
        onBlur={onBlur}
        inputMode="numeric"
        autoComplete="off"
        placeholder={t('dateTime.placeholder')}
        aria-invalid={invalid || undefined}
        aria-describedby={describedBy}
        className={cn(
          'h-11 w-full min-w-0 rounded-lg border border-input bg-transparent py-1 pr-11 pl-2.5 text-[15px] tabular-nums transition-colors outline-none placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 aria-invalid:border-destructive aria-invalid:ring-3 aria-invalid:ring-destructive/20 md:h-10 dark:bg-input/30',
        )}
      />
      <Popover open={open} onOpenChange={changeOpen}>
        <PopoverTrigger asChild>
          <button
            type="button"
            aria-label={t('dateTime.openCalendar')}
            className="absolute top-1/2 right-1.5 flex size-8 -translate-y-1/2 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-accent hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
          >
            <CalendarIcon className="size-4" />
          </button>
        </PopoverTrigger>
        <PopoverContent align="end" className="w-auto gap-0 p-0">
          <Calendar
            mode="single"
            locale={ptBR}
            selected={draftDay}
            onSelect={setDraftDay}
            defaultMonth={draftDay}
            captionLayout="dropdown"
            className="[--cell-size:--spacing(9)]"
          />
          <div className="flex items-end gap-2 border-t p-3">
            <label className="flex flex-1 flex-col gap-1 text-sm font-medium">
              {t('dateTime.time')}
              <input
                value={draftTime}
                onChange={(event) => setDraftTime(maskTime(event.target.value))}
                onKeyDown={(event) => {
                  if (event.key === 'Enter') {
                    event.preventDefault()
                    apply()
                  }
                }}
                inputMode="numeric"
                placeholder="20:00"
                className="h-9 w-full rounded-md border bg-transparent px-2.5 text-sm font-normal tabular-nums focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
              />
            </label>
            <AppButton type="button" size="sm" disabled={!draftDay || !isValidTime(draftTime)} onClick={apply}>
              {t('dateTime.apply')}
            </AppButton>
          </div>
        </PopoverContent>
      </Popover>
    </div>
  )
}
