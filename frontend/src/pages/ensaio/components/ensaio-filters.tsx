import { SlidersHorizontalIcon } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { AppButton } from '@/components/app-button'
import { AppCitySelect } from '@/components/app-city-select'
import { Checkbox } from '@/components/ui/checkbox'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Sheet, SheetContent, SheetDescription, SheetFooter, SheetHeader, SheetTitle } from '@/components/ui/sheet'
import { cn } from '@/lib/utils'
import { searchCities } from '@/pages/perfil/services/cidades-service'
import { allMusicGenres, useMusicGenreLabel } from '@/pages/perfil/genres'
import type { MusicGenre } from '@/pages/perfil/genres'

import { allInstruments, chipInstruments, useInstrumentLabel } from '../ensaio-status'
import type { Instrument } from '../ensaio-status'
import { emptyFilters } from '../services/ensaio-service'
import type { EnsaioFilters as Filters } from '../services/ensaio-service'

const any = 'any'

type EnsaioFiltersProps = {
  value: Filters
  onChange: (value: Filters) => void
  withSamples?: boolean
}

export function EnsaioFilters({ value, onChange, withSamples = false }: EnsaioFiltersProps) {
  const { t } = useTranslation('ensaio')
  const instrumentLabel = useInstrumentLabel()
  const [open, setOpen] = useState(false)
  const extraActive = Boolean(value.city || value.abc || value.genre !== null || value.withSamples || (value.instrument !== null && !chipInstruments.includes(value.instrument)))

  const chip = (active: boolean) =>
    cn(
      'h-11 rounded-full px-[18px] text-[15px] transition-colors focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none',
      active ? 'bg-paper font-semibold text-paper-foreground' : 'border border-chip-border text-foreground hover:bg-paper/10',
    )

  return (
    <div className="-mx-4 flex gap-2 overflow-x-auto px-4 pb-1 md:mx-0 md:flex-wrap md:overflow-visible md:px-0">
      <button type="button" aria-pressed={value.instrument === null} className={chip(value.instrument === null)} onClick={() => onChange({ ...value, instrument: null })}>
        {t('filters.all')}
      </button>
      {chipInstruments.map((instrument) => (
        <button
          key={instrument}
          type="button"
          aria-pressed={value.instrument === instrument}
          className={cn(chip(value.instrument === instrument), 'shrink-0')}
          onClick={() => onChange({ ...value, instrument })}
        >
          {instrumentLabel(instrument)}
        </button>
      ))}
      <button type="button" className={cn(chip(extraActive), 'flex shrink-0 items-center gap-2')} onClick={() => setOpen(true)}>
        <SlidersHorizontalIcon className="size-4" aria-hidden="true" />
        {t('filters.more')}
      </button>

      <Sheet open={open} onOpenChange={setOpen}>
        <SheetContent side="right" className="flex w-full flex-col gap-0 sm:max-w-sm">
          <SheetHeader>
            <SheetTitle>{t('filters.moreTitle')}</SheetTitle>
            <SheetDescription className="sr-only">{t('filters.moreTitle')}</SheetDescription>
          </SheetHeader>
          <div className="flex flex-1 flex-col gap-5 overflow-y-auto px-4">
            <div className="flex flex-col gap-1.5">
              <Label>{t('filters.otherInstruments')}</Label>
              <Select
                value={value.instrument === null ? any : String(value.instrument)}
                onValueChange={(next) => onChange({ ...value, instrument: next === any ? null : (Number(next) as Instrument) })}
              >
                <SelectTrigger className="h-11 md:h-10" aria-label={t('filters.otherInstruments')}>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={any}>{t('filters.anyInstrument')}</SelectItem>
                  {allInstruments.map((instrument) => (
                    <SelectItem key={instrument} value={String(instrument)}>
                      {instrumentLabel(instrument)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="flex flex-col gap-2">
              <Label>{t('filters.city')}</Label>
              <label className="flex items-center gap-2 text-sm">
                <Checkbox checked={value.abc} onCheckedChange={(checked) => onChange({ ...value, abc: checked === true, city: null })} />
                {t('filters.abc')}
              </label>
              {value.abc ? null : (
                <AppCitySelect
                  value={{ city: value.city, cityName: '' }}
                  onChange={(next) => onChange({ ...value, city: next.city })}
                  search={searchCities}
                  placeholder={t('filters.anyCity')}
                  clearLabel={t('filters.anyCity')}
                  ariaLabel={t('filters.city')}
                />
              )}
            </div>

            <div className="flex flex-col gap-1.5">
              <Label>{t('filters.genre')}</Label>
              <GenreSelect value={value.genre} onChange={(genre) => onChange({ ...value, genre })} />
            </div>

            {withSamples ? (
              <label className="flex items-center gap-2 text-sm">
                <Checkbox checked={value.withSamples} onCheckedChange={(checked) => onChange({ ...value, withSamples: checked === true })} />
                {t('filters.withSamples')}
              </label>
            ) : null}
          </div>
          <SheetFooter className="flex-row gap-2">
            <AppButton intent="ghost" className="flex-1" onClick={() => onChange(emptyFilters)}>
              {t('filters.clear')}
            </AppButton>
            <AppButton className="flex-1" onClick={() => setOpen(false)}>
              {t('filters.apply')}
            </AppButton>
          </SheetFooter>
        </SheetContent>
      </Sheet>
    </div>
  )
}

function GenreSelect({ value, onChange }: { value: MusicGenre | null; onChange: (genre: MusicGenre | null) => void }) {
  const { t } = useTranslation('ensaio')
  const genreLabel = useMusicGenreLabel()

  return (
    <Select value={value === null ? any : String(value)} onValueChange={(next) => onChange(next === any ? null : (Number(next) as MusicGenre))}>
      <SelectTrigger className="h-11 md:h-10" aria-label={t('filters.genre')}>
        <SelectValue />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value={any}>{t('filters.anyGenre')}</SelectItem>
        {allMusicGenres.map((genre) => (
          <SelectItem key={genre} value={String(genre)}>
            {genreLabel(genre)}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  )
}
