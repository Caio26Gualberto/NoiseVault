export const ReleaseKind = {
  album: 0,
  ep: 1,
  single: 2,
} as const
export type ReleaseKind = (typeof ReleaseKind)[keyof typeof ReleaseKind]

export const allReleaseKinds: ReleaseKind[] = [ReleaseKind.album, ReleaseKind.ep, ReleaseKind.single]

export const releaseKindKey: Record<ReleaseKind, string> = {
  [ReleaseKind.album]: 'album',
  [ReleaseKind.ep]: 'ep',
  [ReleaseKind.single]: 'single',
}

export const ReleaseSource = {
  spotify: 0,
  manual: 1,
} as const
export type ReleaseSource = (typeof ReleaseSource)[keyof typeof ReleaseSource]

export function spotifyTrackEmbedUrl(trackId: string) {
  return `https://open.spotify.com/embed/track/${encodeURIComponent(trackId)}?utm_source=generator&theme=0`
}

export function spotifyAlbumEmbedUrl(albumId: string) {
  return `https://open.spotify.com/embed/album/${encodeURIComponent(albumId)}?utm_source=generator&theme=0`
}
