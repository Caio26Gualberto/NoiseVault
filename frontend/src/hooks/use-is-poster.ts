import { useEffect, useState } from 'react'

const posterMaxRatio = 1.2

export function useIsPoster(src: string | null | undefined) {
  const [shape, setShape] = useState<{ src: string; poster: boolean } | null>(null)

  useEffect(() => {
    if (!src) return
    let cancelled = false
    const image = new Image()
    image.onload = () => {
      if (!cancelled && image.naturalHeight > 0) setShape({ src, poster: image.naturalWidth / image.naturalHeight < posterMaxRatio })
    }
    image.src = src
    return () => {
      cancelled = true
    }
  }, [src])

  return shape !== null && shape.src === src && shape.poster
}
