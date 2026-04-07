import { ChannelConnection } from './channel-connection'

export class ChannelRegistry {
  private readonly connections = new Map<string, ChannelConnection>()

  register(channelId: string, accessToken: string, refreshToken: string): void {
    if (this.connections.has(channelId)) {
      console.log(`[registry] ${channelId}: already registered, skipping`)
      return
    }
    const conn = new ChannelConnection(channelId, accessToken, refreshToken)
    this.connections.set(channelId, conn)
    conn.start()
    console.log(`[registry] ${channelId}: registered (total: ${this.connections.size})`)
  }

  deregister(channelId: string): void {
    const conn = this.connections.get(channelId)
    if (!conn) {
      console.warn(`[registry] ${channelId}: not found, nothing to deregister`)
      return
    }
    conn.stop()
    this.connections.delete(channelId)
    console.log(`[registry] ${channelId}: deregistered (total: ${this.connections.size})`)
  }

  list(): string[] {
    return Array.from(this.connections.keys())
  }

  size(): number {
    return this.connections.size
  }
}