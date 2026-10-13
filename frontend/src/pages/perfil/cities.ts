import { useTranslation } from 'react-i18next'

export const AbcCity = {
  santoAndre: 0,
  saoBernardo: 1,
  saoCaetano: 2,
  diadema: 3,
  maua: 4,
  ribeiraoPires: 5,
  rioGrandeDaSerra: 6,
  saoPaulo: 7,
  other: 8,
} as const

export type AbcCity = (typeof AbcCity)[keyof typeof AbcCity]

export const abcCityKey: Record<AbcCity, string> = {
  [AbcCity.santoAndre]: 'santoAndre',
  [AbcCity.saoBernardo]: 'saoBernardo',
  [AbcCity.saoCaetano]: 'saoCaetano',
  [AbcCity.diadema]: 'diadema',
  [AbcCity.maua]: 'maua',
  [AbcCity.ribeiraoPires]: 'ribeiraoPires',
  [AbcCity.rioGrandeDaSerra]: 'rioGrandeDaSerra',
  [AbcCity.saoPaulo]: 'saoPaulo',
  [AbcCity.other]: 'other',
}

export const allAbcCities = Object.values(AbcCity) as AbcCity[]

export function useAbcCityLabel() {
  const { t } = useTranslation('perfil')
  return (city: AbcCity) => t(`cities.${abcCityKey[city]}`)
}
