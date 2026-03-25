import { subEventsRepository, followEventsRepository, cheerEventsRepository, raidEventsRepository, ytSuperChatEventsRepository, ytMemberEventsRepository } from "@/repositories"
import type { LiveEvent, LiveEventType } from "@/types/events"
import type { EventFilter, PaginatedEvents } from "@/types/event-filter"

class LiveEventFeedService {
  async getFilteredEvents(filter: EventFilter): Promise<PaginatedEvents> {
    const { broadcasterId, youtubeChannelId, types, from, to, sortBy = "occurredAt", sortOrder = "desc", page = 1, limit = 25 } = filter
    const since = from ?? new Date(0)
    const until = to ?? new Date()

    const include = (type: LiveEventType) => !types || types.includes(type)

    const [subs, follows, cheers, raids, superchats, members] = await Promise.all([
      include("sub")       ? subEventsRepository.findInRange(broadcasterId, since, until) : [],
      include("follow")    ? followEventsRepository.findInRange(broadcasterId, since, until) : [],
      include("bits")      ? cheerEventsRepository.findInRange(broadcasterId, since, until) : [],
      include("raid")      ? raidEventsRepository.findInRange(broadcasterId, since, until) : [],
      include("superchat") && youtubeChannelId ? ytSuperChatEventsRepository.findInRange(youtubeChannelId, since, until) : [],
      include("member")    && youtubeChannelId ? ytMemberEventsRepository.findInRange(youtubeChannelId, since, until) : [],
    ])

    const all: LiveEvent[] = [
      ...subs.map(e => ({ id: e.id, type: "sub" as const, platform: "twitch" as const, fromUser: e.userDisplayName ?? e.gifterDisplayName ?? "Anonymous", amount: e.giftCount, occurredAt: e.occurredAt.toISOString(), tier: e.tier, subKind: e.kind, cumulativeMonths: e.cumulativeMonths ?? null, message: e.message ?? null })),
      ...follows.map(e => ({ id: e.id, type: "follow" as const, platform: "twitch" as const, fromUser: e.userDisplayName ?? "Anonymous", amount: null, occurredAt: e.occurredAt.toISOString() })),
      ...cheers.map(e => ({ id: e.id, type: "bits" as const, platform: "twitch" as const, fromUser: e.isAnonymous ? "Anonymous" : (e.userDisplayName ?? "Anonymous"), amount: e.bits, occurredAt: e.occurredAt.toISOString(), message: e.message ?? null, isAnonymous: e.isAnonymous })),
      ...raids.map(e => ({ id: e.id, type: "raid" as const, platform: "twitch" as const, fromUser: e.fromBroadcasterDisplayName, amount: e.viewerCount, occurredAt: e.occurredAt.toISOString() })),
      ...superchats.map(e => ({ id: e.id, type: "superchat" as const, platform: "youtube" as const, fromUser: e.userDisplayName ?? "Anonymous", amount: e.amountMicros / 1_000_000, currency: e.currency, occurredAt: e.occurredAt.toISOString(), message: e.message ?? null })),
      ...members.map(e => ({ id: e.id, type: "member" as const, platform: "youtube" as const, fromUser: e.userDisplayName ?? "Anonymous", amount: e.memberMonths, occurredAt: e.occurredAt.toISOString(), levelName: e.levelName ?? null })),
    ]

    all.sort((a, b) => {
      if (sortBy === "amount") {
        const diff = (b.amount ?? 0) - (a.amount ?? 0)
        return sortOrder === "asc" ? -diff : diff
      }
      const diff = new Date(b.occurredAt).getTime() - new Date(a.occurredAt).getTime()
      return sortOrder === "asc" ? -diff : diff
    })

    const total = all.length
    const totalPages = Math.ceil(total / limit)
    const events = all.slice((page - 1) * limit, page * limit)

    return { events, total, page, totalPages }
  }

  async getEventsSince(broadcasterId: string, since: Date, youtubeChannelId?: string | null): Promise<LiveEvent[]> {
    const [subs, follows, cheers, raids, superchats, members] = await Promise.all([
      subEventsRepository.findSince(broadcasterId, since),
      followEventsRepository.findSince(broadcasterId, since),
      cheerEventsRepository.findSince(broadcasterId, since),
      raidEventsRepository.findSince(broadcasterId, since),
      youtubeChannelId ? ytSuperChatEventsRepository.findSince(youtubeChannelId, since) : [],
      youtubeChannelId ? ytMemberEventsRepository.findSince(youtubeChannelId, since) : [],
    ])

    const events: LiveEvent[] = [
      ...subs.map(e => ({ id: e.id, type: "sub" as const, platform: "twitch" as const, fromUser: e.userDisplayName ?? e.gifterDisplayName ?? "Anonymous", amount: e.giftCount, occurredAt: e.occurredAt.toISOString(), tier: e.tier, subKind: e.kind, cumulativeMonths: e.cumulativeMonths ?? null, message: e.message ?? null })),
      ...follows.map(e => ({ id: e.id, type: "follow" as const, platform: "twitch" as const, fromUser: e.userDisplayName ?? "Anonymous", amount: null, occurredAt: e.occurredAt.toISOString() })),
      ...cheers.map(e => ({ id: e.id, type: "bits" as const, platform: "twitch" as const, fromUser: e.isAnonymous ? "Anonymous" : (e.userDisplayName ?? "Anonymous"), amount: e.bits, occurredAt: e.occurredAt.toISOString(), message: e.message ?? null, isAnonymous: e.isAnonymous })),
      ...raids.map(e => ({ id: e.id, type: "raid" as const, platform: "twitch" as const, fromUser: e.fromBroadcasterDisplayName, amount: e.viewerCount, occurredAt: e.occurredAt.toISOString() })),
      ...superchats.map(e => ({ id: e.id, type: "superchat" as const, platform: "youtube" as const, fromUser: e.userDisplayName ?? "Anonymous", amount: e.amountMicros / 1_000_000, currency: e.currency, occurredAt: e.occurredAt.toISOString(), message: e.message ?? null })),
      ...members.map(e => ({ id: e.id, type: "member" as const, platform: "youtube" as const, fromUser: e.userDisplayName ?? "Anonymous", amount: e.memberMonths, occurredAt: e.occurredAt.toISOString(), levelName: e.levelName ?? null })),
    ]

    return events.sort((a, b) => new Date(b.occurredAt).getTime() - new Date(a.occurredAt).getTime())
  }
}

export const liveEventFeedService = new LiveEventFeedService()