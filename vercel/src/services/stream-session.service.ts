import { streamSessionRepository } from "@/repositories/stream-session.repository"

class StreamSessionService {
  async handleOnline(broadcasterId: string, occurredAt: Date) {
    const open = await streamSessionRepository.findOpen(broadcasterId)
    if (!open) {
      await streamSessionRepository.create(broadcasterId, occurredAt)
    }
  }

  async handleOffline(broadcasterId: string, occurredAt: Date) {
    await streamSessionRepository.close(broadcasterId, occurredAt)
  }
}

export const streamSessionService = new StreamSessionService()
