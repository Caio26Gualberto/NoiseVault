import { z } from 'zod'

import type { AppCity, AppCityValue } from '@/components/app-city-select'

export type CityDto = AppCity
export type CityValue = AppCityValue

export const emptyCityValue: CityValue = { city: null, cityName: '' }

export function formatCityDto(city: CityDto) {
  return `${city.name}, ${city.uf}`
}

export function formatCity(place: { city: CityDto | null; cityName: string | null }) {
  if (place.city) return formatCityDto(place.city)
  return place.cityName?.trim() || null
}

export function toCityValue(place: { city: CityDto | null; cityName: string | null }): CityValue {
  return { city: place.city, cityName: place.city ? '' : (place.cityName ?? '') }
}

export function toCityInput(value: CityValue) {
  return {
    cityId: value.city?.id ?? null,
    cityName: value.city ? null : value.cityName.trim() || null,
  }
}

export const cityValueSchema = z.object({
  city: z.object({ id: z.number(), name: z.string(), uf: z.string() }).nullable(),
  cityName: z.string().trim().max(80),
})
