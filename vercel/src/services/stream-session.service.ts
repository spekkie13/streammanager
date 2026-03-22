import { streamSessionRepository } from "@/repositories/stream-session.repository"

class StreamSessionService {
  async handleOnline(broadcasterId: string, occurredAt: Date) : Promise<void> {
    const open = await streamSessionRepository.findOpen(broadcasterId)
    if (!open) {
      await streamSessionRepository.create(broadcasterId, occurredAt)
    }
  }

  async handleOffline(broadcasterId: string, occurredAt: Date) : Promise<void> {
    await streamSessionRepository.close(broadcasterId, occurredAt)
  }
}

export const streamSessionService = new StreamSessionService()
