import { useEffect, useRef } from 'react'

export function usePolling(tick: () => void | Promise<void>, intervalMs: number) {
  const savedTick = useRef(tick)

  useEffect(() => {
    savedTick.current = tick
  }, [tick])

  useEffect(() => {
    if (intervalMs <= 0) return

    let timer: number | undefined

    const run = () => {
      void savedTick.current()
    }

    const start = () => {
      if (timer !== undefined) return
      timer = window.setInterval(run, intervalMs)
    }

    const stop = () => {
      if (timer === undefined) return
      window.clearInterval(timer)
      timer = undefined
    }

    const handleVisibility = () => {
      if (document.hidden) {
        stop()
        return
      }
      run()
      start()
    }

    if (!document.hidden) start()
    document.addEventListener('visibilitychange', handleVisibility)

    return () => {
      stop()
      document.removeEventListener('visibilitychange', handleVisibility)
    }
  }, [intervalMs])
}
