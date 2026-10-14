import type { HubConnection } from '@microsoft/signalr'

import { apiBaseUrl, getFreshToken } from './client'

export type HubState = 'connected' | 'disconnected'

type HubHandlers = {
  onUnread: (count: number) => void
  onStateChange: (state: HubState) => void
}

type UnreadPayload = { unreadCount: number }

const hubPath = '/hubs/notifications'

export function connectNotificationsHub(handlers: HubHandlers): () => void {
  let disposed = false
  let connection: HubConnection | null = null

  void import('@microsoft/signalr').then(async ({ HubConnectionBuilder, HttpTransportType, LogLevel }) => {
    if (disposed) return

    const hub = new HubConnectionBuilder()
      .withUrl(`${apiBaseUrl}${hubPath}`, {
        skipNegotiation: true,
        transport: HttpTransportType.WebSockets,
        accessTokenFactory: async () => (await getFreshToken()) ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.None)
      .build()

    connection = hub

    hub.on('unread', (payload: UnreadPayload) => handlers.onUnread(payload.unreadCount))
    hub.onreconnecting(() => handlers.onStateChange('disconnected'))
    hub.onreconnected(() => handlers.onStateChange('connected'))
    hub.onclose(() => {
      if (!disposed) handlers.onStateChange('disconnected')
    })

    try {
      await hub.start()
      if (disposed) {
        await hub.stop()
        return
      }
      handlers.onStateChange('connected')
    } catch {
      if (!disposed) handlers.onStateChange('disconnected')
    }
  })

  return () => {
    disposed = true
    void connection?.stop()
  }
}
