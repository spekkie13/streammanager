import { io, Socket } from 'socket.io-client'
import { creatorDeckClient, type IncomingChatMessage } from './creatordeck-client'

const SE_REALTIME_URL = 'https://realtime.streamelements.com'
const TOKEN_REFRESH_INTERVAL_MS = 6 * 24 * 60 * 60 * 1000 + 20 * 60 * 60 * 1000 // 6d 20h
const MAX_RECONNECT_ATTEMPTS = 5
const RECONNECT_BASE_DELAY_MS = 1000

export class ChannelConnection {
  private socket: Socket | null = null
  private reconnectAttempts = 0
  private refreshTimer: NodeJS.Timeout | null = null

  constructor(
    readonly channelId: string,
    private accessToken: string,
    private refreshToken: string,
  ) {}

  start(): void {
    this.connect()
    this.scheduleTokenRefresh()
  }

  stop(): void {
    if (this.refreshTimer) clearTimeout(this.refreshTimer)
    if (this.socket) {
      this.socket.removeAllListeners()
      this.socket.disconnect()
      this.socket = null
    }
    console.log(`[bridge] ${this.channelId}: stopped`)
  }

  private connect(): void {
    this.socket = io(SE_REALTIME_URL, { transports: ['websocket'] })

    this.socket.on('connect', () => {
      console.log(`[bridge] ${this.channelId}: connected to SE`)
      this.reconnectAttempts = 0
      this.socket!.emit('authenticate', { method: 'oauth2', token: this.accessToken })
    })

    this.socket.on('authenticated', () => {
      console.log(`[bridge] ${this.channelId}: authenticated`)
    })

    this.socket.on('unauthorized', (err: unknown) => {
      console.error(`[bridge] ${this.channelId}: unauthorized —`, err)
    })

    this.socket.on('event', (data: Record<string, unknown>) => {
      if (data['topic'] !== 'channel.chat.message') return
      const msg = this.extractMessage(data)
      if (!msg) return
      creatorDeckClient.ingestMessages(this.channelId, [msg]).catch(err =>
        console.error(`[bridge] ${this.channelId}: ingest failed —`, err),
      )
    })

    this.socket.on('disconnect', (reason: string) => {
      console.warn(`[bridge] ${this.channelId}: disconnected (${reason})`)
      this.scheduleReconnect()
    })

    this.socket.on('connect_error', (err: Error) => {
      console.error(`[bridge] ${this.channelId}: connection error —`, err.message)
      this.scheduleReconnect()
    })
  }

  private scheduleReconnect(): void {
    if (this.reconnectAttempts >= MAX_RECONNECT_ATTEMPTS) {
      console.error(`[bridge] ${this.channelId}: max reconnect attempts reached, giving up`)
      return
    }
    const delay = RECONNECT_BASE_DELAY_MS * Math.pow(2, this.reconnectAttempts)
    this.reconnectAttempts++
    console.log(`[bridge] ${this.channelId}: reconnecting in ${delay}ms (attempt ${this.reconnectAttempts})`)
    setTimeout(() => {
      if (this.socket) {
        this.socket.removeAllListeners()
        this.socket.disconnect()
      }
      this.connect()
    }, delay)
  }

  private scheduleTokenRefresh(): void {
    this.refreshTimer = setTimeout(async () => {
      console.log(`[bridge] ${this.channelId}: refreshing SE token`)
      const tokens = await creatorDeckClient.refreshSeToken(this.refreshToken)
      if (!tokens) {
        console.error(`[bridge] ${this.channelId}: token refresh failed`)
        return
      }
      this.accessToken = tokens.accessToken
      this.refreshToken = tokens.refreshToken
      await creatorDeckClient.updateToken(this.channelId, tokens.accessToken).catch(err =>
        console.error(`[bridge] ${this.channelId}: updateToken failed —`, err),
      )
      // Reconnect with new token
      this.stop()
      this.start()
    }, TOKEN_REFRESH_INTERVAL_MS)
  }

  private extractMessage(data: Record<string, unknown>): IncomingChatMessage | null {
    const payload = data['data'] as Record<string, unknown> | undefined
    const snippet = payload?.['snippet'] as Record<string, unknown> | undefined
    const authorDetails = payload?.['authorDetails'] as Record<string, unknown> | undefined

    const message =
      (snippet?.['displayMessage'] as string | undefined) ??
      ((snippet?.['textMessageDetails'] as Record<string, unknown> | undefined)?.['messageText'] as string | undefined)

    if (!message) return null

    return {
      id: (data['id'] as string | undefined) ?? `${this.channelId}-${Date.now()}`,
      userDisplayName: (authorDetails?.['displayName'] as string | undefined) ?? 'Unknown',
      userId: (authorDetails?.['channelId'] as string | undefined) ?? null,
      message,
      occurredAt: (data['ts'] as string | undefined) ?? new Date().toISOString(),
    }
  }
}