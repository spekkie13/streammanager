import { subEventsRepository, followEventsRepository, cheerEventsRepository, raidEventsRepository } from "@/repositories"
import type { LiveEvent } from "@/types/events"

class LiveEventFeedService {
  async getEventsSince(broadcasterId: string, since: Date): Promise<LiveEvent[]> {
    const [subs, follows, cheers, raids] = await Promise.all([
      subEventsRepository.findSince(broadcasterId, since),
      followEventsRepository.findSince(broadcasterId, since),
      cheerEventsRepository.findSince(broadcasterId, since),
      raidEventsRepository.findSince(broadcasterId, since),
    ])

    const events: LiveEvent[] = [
      ...subs.map(e => ({
        id: e.id,
        type: "sub" as const,
        fromUser: e.userDisplayName ?? e.gifterDisplayName ?? "Anonymous",
        amount: e.giftCount,
        occurredAt: e.occurredAt.toISOString(),
      })),
      ...follows.map(e => ({
        id: e.id,
        type: "follow" as const,
        fromUser: e.userDisplayName ?? "Anonymous",
        amount: null,
        occurredAt: e.occurredAt.toISOString(),
      })),
      ...cheers.map(e => ({
        id: e.id,
        type: "bits" as const,
        fromUser: e.isAnonymous ? "Anonymous" : (e.userDisplayName ?? "Anonymous"),
        amount: e.bits,
        occurredAt: e.occurredAt.toISOString(),
      })),
      ...raids.map(e => ({
        id: e.id,
        type: "raid" as const,
        fromUser: e.fromBroadcasterDisplayName,
        amount: e.viewerCount,
        occurredAt: e.occurredAt.toISOString(),
      })),
    ]

    return events.sort((a, b) => new Date(b.occurredAt).getTime() - new Date(a.occurredAt).getTime())
  }
}

export const liveEventFeedService = new LiveEventFeedService()