import { useState } from 'react'

import { useAuth } from '@/auth/use-auth'
import { clearApproxLocation, setApproxLocation } from '@/pages/minha-conta/services/conta-service'

export type ApproxLocationState = 'idle' | 'locating' | 'denied' | 'unavailable'

const gridDecimals = 2

function snap(value: number) {
  return Number(value.toFixed(gridDecimals))
}

function currentPosition() {
  return new Promise<GeolocationPosition>((resolve, reject) =>
    navigator.geolocation.getCurrentPosition(resolve, reject, { enableHighAccuracy: false, timeout: 15000, maximumAge: 10 * 60 * 1000 }),
  )
}

export function useApproxLocation() {
  const { user, replaceUser } = useAuth()
  const [state, setState] = useState<ApproxLocationState>('idle')

  const enable = async () => {
    if (!('geolocation' in navigator)) {
      setState('unavailable')
      return false
    }

    setState('locating')

    let position: GeolocationPosition
    try {
      position = await currentPosition()
    } catch (error) {
      setState((error as GeolocationPositionError).code === 1 ? 'denied' : 'unavailable')
      return false
    }

    try {
      replaceUser(await setApproxLocation(snap(position.coords.latitude), snap(position.coords.longitude)))
      setState('idle')
      return true
    } catch {
      setState('idle')
      return false
    }
  }

  const disable = async () => {
    try {
      replaceUser(await clearApproxLocation())
    } catch {
      return
    }
  }

  return { enabled: user?.hasApproxLocation ?? false, state, enable, disable }
}
