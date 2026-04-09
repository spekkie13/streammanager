import { ChannelConnection } from './channel-connection'

export class ChannelRegistry {
  private readonly connections = new Map<string, ChannelConnection>()

  register(channelId: string, jwtToken: string): void {
    if (this.connections.has(channelId)) {
      console.log(`[registry] ${channelId}: already registered, skipping`)
      return
    }
    const conn = new ChannelConnection(channelId, jwtToken)
    this.connections.set(channelId, conn)
    conn.start()
    console.log(`[registry] ${channelId}: registered (total: ${this.connections.size})`)
  }

  deregister(channelId: string): void {
    const conn = this.connections.get(channelId)
    if (!conn) return
    conn.stop()
    this.connections.delete(channelId)
    console.log(`[registry] ${channelId}: deregistered (total: ${this.connections.size})`)
  }

  activeChannelIds(): Set<string> {
    return new Set(this.connections.keys())
  }
}