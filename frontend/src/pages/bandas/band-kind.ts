export const BandKind = {
  band: 0,
  solo: 1,
} as const
export type BandKind = (typeof BandKind)[keyof typeof BandKind]

export const allBandKinds: BandKind[] = [BandKind.band, BandKind.solo]

export const bandKindKey: Record<BandKind, string> = {
  [BandKind.band]: 'band',
  [BandKind.solo]: 'solo',
}

const bandKindSection: Record<BandKind, string> = {
  [BandKind.band]: 'bandas',
  [BandKind.solo]: 'artistas',
}

export const isSolo = (kind: BandKind | undefined) => kind === BandKind.solo

export const bandKindContext = (kind: BandKind | undefined) => (isSolo(kind) ? 'solo' : undefined)

export const bandSectionPath = (kind: BandKind | undefined) => `/${bandKindSection[kind ?? BandKind.band]}`

export const bandPagePath = (band: { slug: string; kind?: BandKind }) => `${bandSectionPath(band.kind)}/${band.slug}`

export const bandEditPath = (band: { slug: string; kind?: BandKind }) => `${bandPagePath(band)}/editar`

export const bandCreatePath = (kind: BandKind) => `${bandSectionPath(kind)}/nova`

export const canonicalBandLocation = (pathname: string, kind: BandKind | undefined) => {
  const section = bandSectionPath(kind)
  return pathname.startsWith(`${section}/`) ? null : pathname.replace(/^\/[^/]+/, section)
}
