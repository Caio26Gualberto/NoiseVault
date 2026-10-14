import { useCallback, useEffect, useRef, useState } from 'react'
import type { RefObject } from 'react'
import type Hls from 'hls.js'

export type VideoQuality = {
  index: number
  lines: number
  frameRate: number
}

const highFrameRate = 30.5

export function qualityLabel(quality: VideoQuality): string {
  return quality.frameRate > highFrameRate ? `${quality.lines}p${Math.round(quality.frameRate)}` : `${quality.lines}p`
}

function isPlaylist(src: string): boolean {
  return src.split('?')[0].toLowerCase().endsWith('.m3u8')
}

export function useVideoSource(videoRef: RefObject<HTMLVideoElement | null>, src: string) {
  const hlsRef = useRef<Hls | null>(null)
  const [qualities, setQualities] = useState<VideoQuality[]>([])
  const [selected, setSelected] = useState(-1)
  const [active, setActive] = useState<number | null>(null)

  useEffect(() => {
    const video = videoRef.current
    if (!video) return

    let disposed = false

    const playNatively = () => {
      video.src = src
    }

    if (!isPlaylist(src)) {
      playNatively()
    } else {
      void import('hls.js')
        .then(({ default: HlsPlayer }) => {
          if (disposed) return
          if (!HlsPlayer.isSupported()) {
            playNatively()
            return
          }

          const hls = new HlsPlayer({ capLevelToPlayerSize: true })
          hlsRef.current = hls

          hls.on(HlsPlayer.Events.MANIFEST_PARSED, (_, data) => {
            setQualities(
              data.levels
                .map((level, index) => ({
                  index,
                  lines: Math.min(level.width, level.height) || level.height,
                  frameRate: level.frameRate ?? 0,
                }))
                .sort((a, b) => b.lines - a.lines),
            )
          })
          hls.on(HlsPlayer.Events.LEVEL_SWITCHED, (_, data) => setActive(data.level))
          hls.loadSource(src)
          hls.attachMedia(video)
        })
        .catch(playNatively)
    }

    return () => {
      disposed = true
      hlsRef.current?.destroy()
      hlsRef.current = null
      setQualities([])
      setSelected(-1)
      setActive(null)
      video.removeAttribute('src')
    }
  }, [src, videoRef])

  const selectQuality = useCallback((index: number) => {
    const hls = hlsRef.current
    if (!hls) return
    hls.currentLevel = index
    setSelected(index)
  }, [])

  const activeQuality = qualities.find((quality) => quality.index === active) ?? null

  return { qualities, selected, activeQuality, selectQuality }
}
