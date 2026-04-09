import { io, Socket } from 'socket.io-client'
import { creatorDeckClient } from './creatordeck-client'

const SE_REALTIME_URL = 'https://realtime.streamelements.com'
const MAX_RECONNECT_ATTEMPTS = 5
const RECONNECT_BASE_DELAY_MS = 1000

export class ChannelConnection {
  private socket: Socket | null = null
  private reconnectAttempts = 0

  constructor(
    readonly channelId: string,
    private readonly jwtToken: string,
  ) {}

  start(): void {
    this.connect()
  }

  stop(): void {
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
      this.socket!.emit('authenticate', { method: 'jwt', token: this.jwtToken })
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
      creatorDeckClient
        .forwardMessage(this.channelId, msg.eventId, msg.userDisplayName, msg.userId, msg.message, msg.occurredAt)
        .catch(err => console.error(`[bridge] ${this.channelId}: forward failed —`, err))
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

  private extractMessage(data: Record<string, unknown>): {
    eventId: string
    userDisplayName: string | null
    userId: string | null
    message: string
    occurredAt: string
  } | null {
    const payload = data['data'] as Record<string, unknown> | undefined
    const snippet = payload?.['snippet'] as Record<string, unknown> | undefined
    const authorDetails = payload?.['authorDetails'] as Record<string, unknown> | undefined

    const message =
      (snippet?.['displayMessage'] as string | undefined) ??
      ((snippet?.['textMessageDetails'] as Record<string, unknown> | undefined)?.['messageText'] as string | undefined)

    if (!message) return null

    return {
      eventId: (data['id'] as string | undefined) ?? `${this.channelId}-${Date.now()}`,
      userDisplayName: (authorDetails?.['displayName'] as string | undefined) ?? null,
      userId: (authorDetails?.['channelId'] as string | undefined) ?? null,
      message,
      occurredAt: (data['ts'] as string | undefined) ?? new Date().toISOString(),
    }
  }
}