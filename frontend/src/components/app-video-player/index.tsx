import {
  LoaderCircleIcon,
  MaximizeIcon,
  MinimizeIcon,
  PauseIcon,
  PlayIcon,
  RotateCcwIcon,
  Volume1Icon,
  Volume2Icon,
  VolumeXIcon,
} from 'lucide-react'
import { useCallback, useEffect, useRef, useState } from 'react'
import type { KeyboardEvent, PointerEvent, ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import { formatDuration } from '@/lib/format'
import { cn } from '@/lib/utils'

import { QualityMenu } from './quality-menu'
import { useVideoSource } from './use-video-source'

type AppVideoPlayerProps = {
  src: string
  poster?: string | null
  autoPlay?: boolean
  className?: string
}

type FullscreenVideo = HTMLVideoElement & { webkitEnterFullscreen?: () => void }

const speeds = [1, 1.25, 1.5, 2, 0.5]
const hideAfterMs = 2500
const seekStep = 5
const volumeStep = 0.1

function clamp(value: number, min: number, max: number) {
  return Math.min(Math.max(value, min), max)
}

function ControlButton({ label, onClick, children }: { label: string; onClick: () => void; children: ReactNode }) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={label}
      title={label}
      className="flex size-9 shrink-0 items-center justify-center rounded-full text-on-scrim transition-colors hover:bg-on-scrim/15 focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none [&_svg]:size-5"
    >
      {children}
    </button>
  )
}

export function AppVideoPlayer({ src, poster, autoPlay = false, className }: AppVideoPlayerProps) {
  const { t } = useTranslation('common')
  const frameRef = useRef<HTMLDivElement>(null)
  const videoRef = useRef<HTMLVideoElement>(null)
  const trackRef = useRef<HTMLDivElement>(null)
  const hideTimer = useRef<number | undefined>(undefined)
  const [playing, setPlaying] = useState(false)
  const [ended, setEnded] = useState(false)
  const [waiting, setWaiting] = useState(false)
  const [current, setCurrent] = useState(0)
  const [duration, setDuration] = useState(0)
  const [buffered, setBuffered] = useState(0)
  const [volume, setVolume] = useState(1)
  const [muted, setMuted] = useState(false)
  const [speed, setSpeed] = useState(1)
  const [fullscreen, setFullscreen] = useState(false)
  const [controlsVisible, setControlsVisible] = useState(true)
  const [scrubbing, setScrubbing] = useState(false)
  const [hover, setHover] = useState<{ x: number; time: number } | null>(null)
  const [menuOpen, setMenuOpen] = useState(false)
  const source = useVideoSource(videoRef, src)

  const showControls = useCallback(() => {
    setControlsVisible(true)
    window.clearTimeout(hideTimer.current)
    hideTimer.current = window.setTimeout(() => {
      if (videoRef.current && !videoRef.current.paused) setControlsVisible(false)
    }, hideAfterMs)
  }, [])

  useEffect(() => () => window.clearTimeout(hideTimer.current), [])

  useEffect(() => {
    const onChange = () => setFullscreen(document.fullscreenElement === frameRef.current)
    document.addEventListener('fullscreenchange', onChange)
    return () => document.removeEventListener('fullscreenchange', onChange)
  }, [])

  const togglePlay = useCallback(() => {
    const video = videoRef.current
    if (!video) return
    if (video.paused || video.ended) void video.play().catch(() => undefined)
    else video.pause()
    showControls()
  }, [showControls])

  const seekTo = useCallback((time: number) => {
    const video = videoRef.current
    if (!video || !Number.isFinite(video.duration)) return
    video.currentTime = clamp(time, 0, video.duration)
    setCurrent(video.currentTime)
  }, [])

  const changeVolume = useCallback((value: number) => {
    const video = videoRef.current
    if (!video) return
    video.volume = clamp(value, 0, 1)
    video.muted = video.volume === 0
  }, [])

  const toggleMute = useCallback(() => {
    const video = videoRef.current
    if (!video) return
    video.muted = !video.muted
    if (!video.muted && video.volume === 0) video.volume = 0.5
  }, [])

  const toggleFullscreen = useCallback(() => {
    const frame = frameRef.current
    const video = videoRef.current as FullscreenVideo | null
    if (!frame || !video) return
    if (document.fullscreenElement) {
      void document.exitFullscreen()
      return
    }
    if (frame.requestFullscreen) void frame.requestFullscreen().catch(() => undefined)
    else video.webkitEnterFullscreen?.()
  }, [])

  const cycleSpeed = () => {
    const video = videoRef.current
    if (!video) return
    const next = speeds[(speeds.indexOf(speed) + 1) % speeds.length]
    video.playbackRate = next
    setSpeed(next)
  }

  const timeAt = (clientX: number) => {
    const track = trackRef.current
    if (!track || !duration) return { x: 0, time: 0 }
    const rect = track.getBoundingClientRect()
    const x = clamp(clientX - rect.left, 0, rect.width)
    return { x, time: (x / rect.width) * duration }
  }

  const startScrub = (event: PointerEvent<HTMLDivElement>) => {
    event.currentTarget.setPointerCapture(event.pointerId)
    setScrubbing(true)
    seekTo(timeAt(event.clientX).time)
  }

  const moveScrub = (event: PointerEvent<HTMLDivElement>) => {
    const point = timeAt(event.clientX)
    setHover(point)
    if (scrubbing) seekTo(point.time)
  }

  const endScrub = (event: PointerEvent<HTMLDivElement>) => {
    if (event.currentTarget.hasPointerCapture(event.pointerId)) event.currentTarget.releasePointerCapture(event.pointerId)
    setScrubbing(false)
  }

  const onKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const video = videoRef.current
    if (!video) return
    const actions: Record<string, () => void> = {
      ' ': togglePlay,
      k: togglePlay,
      ArrowRight: () => seekTo(video.currentTime + seekStep),
      ArrowLeft: () => seekTo(video.currentTime - seekStep),
      ArrowUp: () => changeVolume(video.volume + volumeStep),
      ArrowDown: () => changeVolume(video.volume - volumeStep),
      m: toggleMute,
      f: toggleFullscreen,
    }
    const action = actions[event.key]
    if (!action) return
    event.preventDefault()
    event.stopPropagation()
    action()
    showControls()
  }

  const played = duration ? (current / duration) * 100 : 0
  const loaded = duration ? (buffered / duration) * 100 : 0
  const effectiveVolume = muted ? 0 : volume
  const VolumeIcon = effectiveVolume === 0 ? VolumeXIcon : effectiveVolume < 0.5 ? Volume1Icon : Volume2Icon
  const visible = controlsVisible || !playing || scrubbing || menuOpen

  return (
    <div
      ref={frameRef}
      tabIndex={0}
      role="region"
      aria-label={t('player.label')}
      onKeyDown={onKeyDown}
      onPointerMove={showControls}
      onPointerLeave={() => playing && setControlsVisible(false)}
      className={cn(
        'group/player relative flex items-center justify-center overflow-hidden rounded-lg bg-scrim outline-none focus-visible:ring-2 focus-visible:ring-ring',
        fullscreen && 'rounded-none',
        !visible && 'cursor-none',
        className,
      )}
    >
      <video
        ref={videoRef}
        poster={poster ?? undefined}
        autoPlay={autoPlay}
        playsInline
        preload="metadata"
        onClick={togglePlay}
        onDoubleClick={toggleFullscreen}
        onPlay={() => {
          setPlaying(true)
          setEnded(false)
          showControls()
        }}
        onPause={() => {
          setPlaying(false)
          setControlsVisible(true)
        }}
        onEnded={() => {
          setPlaying(false)
          setEnded(true)
          setControlsVisible(true)
        }}
        onWaiting={() => setWaiting(true)}
        onPlaying={() => setWaiting(false)}
        onCanPlay={() => setWaiting(false)}
        onLoadedMetadata={(event) => setDuration(event.currentTarget.duration)}
        onDurationChange={(event) => setDuration(event.currentTarget.duration)}
        onTimeUpdate={(event) => {
          if (!scrubbing) setCurrent(event.currentTarget.currentTime)
        }}
        onProgress={(event) => {
          const ranges = event.currentTarget.buffered
          if (ranges.length > 0) setBuffered(ranges.end(ranges.length - 1))
        }}
        onVolumeChange={(event) => {
          setVolume(event.currentTarget.volume)
          setMuted(event.currentTarget.muted)
        }}
        className={cn('max-h-full w-full', fullscreen ? 'h-full object-contain' : 'max-h-[75vh] object-contain')}
      />

      {waiting && playing ? (
        <span className="pointer-events-none absolute inset-0 flex items-center justify-center">
          <LoaderCircleIcon className="size-12 animate-spin text-on-scrim" />
        </span>
      ) : null}

      {!playing && !waiting ? (
        <button
          type="button"
          onClick={togglePlay}
          aria-label={ended ? t('player.replay') : t('player.play')}
          className="absolute top-1/2 left-1/2 flex size-16 -translate-x-1/2 -translate-y-1/2 items-center justify-center rounded-full bg-primary text-primary-foreground shadow-lg transition-transform hover:scale-105 focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none"
        >
          {ended ? <RotateCcwIcon className="size-7" /> : <PlayIcon className="ml-1 size-7 fill-current" />}
        </button>
      ) : null}

      <div
        className={cn(
          'absolute inset-x-0 bottom-0 flex flex-col gap-1 bg-linear-to-t from-scrim/90 via-scrim/50 to-transparent px-3 pt-10 pb-2 transition-opacity duration-200',
          visible ? 'opacity-100' : 'pointer-events-none opacity-0',
        )}
      >
        <div
          ref={trackRef}
          role="slider"
          tabIndex={-1}
          aria-label={t('player.seek')}
          aria-valuemin={0}
          aria-valuemax={Math.round(duration)}
          aria-valuenow={Math.round(current)}
          aria-valuetext={`${formatDuration(current)} / ${formatDuration(duration)}`}
          onPointerDown={startScrub}
          onPointerMove={moveScrub}
          onPointerUp={endScrub}
          onPointerCancel={endScrub}
          onPointerLeave={() => setHover(null)}
          className="group/track relative flex h-4 cursor-pointer touch-none items-center outline-none"
        >
          <div className="relative h-1 w-full overflow-hidden rounded-full bg-on-scrim/20 transition-[height] group-hover/track:h-1.5">
            <div className="absolute inset-y-0 left-0 bg-on-scrim/35" style={{ width: `${loaded}%` }} />
            <div className="absolute inset-y-0 left-0 bg-primary" style={{ width: `${played}%` }} />
          </div>
          <span
            className={cn(
              'pointer-events-none absolute size-3.5 -translate-x-1/2 rounded-full bg-primary ring-2 ring-on-scrim transition-transform',
              scrubbing ? 'scale-100' : 'scale-0 group-hover/track:scale-100',
            )}
            style={{ left: `${played}%` }}
          />
          {hover && duration ? (
            <span
              className="pointer-events-none absolute bottom-5 -translate-x-1/2 rounded bg-scrim px-1.5 py-0.5 text-xs text-on-scrim tabular-nums"
              style={{ left: hover.x }}
            >
              {formatDuration(hover.time)}
            </span>
          ) : null}
        </div>

        <div className="flex items-center gap-1">
          <ControlButton label={playing ? t('player.pause') : ended ? t('player.replay') : t('player.play')} onClick={togglePlay}>
            {playing ? <PauseIcon className="fill-current" /> : ended ? <RotateCcwIcon /> : <PlayIcon className="fill-current" />}
          </ControlButton>

          <div className="group/volume flex items-center">
            <ControlButton label={effectiveVolume === 0 ? t('player.unmute') : t('player.mute')} onClick={toggleMute}>
              <VolumeIcon />
            </ControlButton>
            <div className="relative flex h-4 w-0 items-center overflow-hidden opacity-0 transition-all group-focus-within/volume:w-20 group-focus-within/volume:opacity-100 group-hover/volume:w-20 group-hover/volume:opacity-100 max-sm:hidden">
              <div className="relative mx-1.5 h-1 flex-1 rounded-full bg-on-scrim/25">
                <div className="absolute inset-y-0 left-0 rounded-full bg-primary" style={{ width: `${effectiveVolume * 100}%` }} />
                <span
                  className="pointer-events-none absolute top-1/2 size-3 -translate-x-1/2 -translate-y-1/2 rounded-full bg-primary ring-2 ring-on-scrim"
                  style={{ left: `${effectiveVolume * 100}%` }}
                />
              </div>
              <input
                type="range"
                min={0}
                max={1}
                step={0.05}
                value={effectiveVolume}
                onChange={(event) => changeVolume(Number(event.target.value))}
                aria-label={t('player.volume')}
                aria-valuetext={`${Math.round(effectiveVolume * 100)}%`}
                className="absolute inset-0 h-full w-full cursor-pointer opacity-0"
              />
            </div>
          </div>

          <span className="ml-2 text-xs font-medium text-on-scrim tabular-nums select-none">
            {formatDuration(current)} <span className="text-on-scrim/60">/ {formatDuration(duration)}</span>
          </span>

          <div className="ml-auto flex items-center gap-1">
            <QualityMenu
              qualities={source.qualities}
              selected={source.selected}
              active={source.activeQuality}
              onSelect={source.selectQuality}
              onOpenChange={setMenuOpen}
            />
            <button
              type="button"
              onClick={cycleSpeed}
              aria-label={t('player.speed', { speed })}
              title={t('player.speed', { speed })}
              className="h-7 min-w-11 rounded-full px-2 font-heading text-sm tracking-wide text-on-scrim tabular-nums transition-colors hover:bg-on-scrim/15 focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none"
            >
              {speed}x
            </button>
            <ControlButton label={fullscreen ? t('player.exitFullscreen') : t('player.fullscreen')} onClick={toggleFullscreen}>
              {fullscreen ? <MinimizeIcon /> : <MaximizeIcon />}
            </ControlButton>
          </div>
        </div>
      </div>
    </div>
  )
}
