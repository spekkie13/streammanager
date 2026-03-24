import { and, desc, eq, gt, gte, lte } from "drizzle-orm"
import { db } from "@/lib/db"
import { followEvents } from "@/lib/schema"
import type { FollowEvent, InsertFollowEvent } from "@/types/entities"

class FollowEventsRepository {
  async insert(data: InsertFollowEvent): Promise<void> {
    await db.insert(followEvents).values(data)
      .onConflictDoUpdate({
        target: [followEvents.broadcasterId, followEvents.userId],
        set: {
          eventId: data.eventId,
          userLogin: data.userLogin,
          userDisplayName: data.userDisplayName,
          occurredAt: data.occurredAt,
        },
      })
  }

  async findSince(broadcasterId: string, since: Date): Promise<FollowEvent[]> {
    return db.select().from(followEvents)
      .where(and(eq(followEvents.broadcasterId, broadcasterId), gt(followEvents.occurredAt, since)))
      .orderBy(desc(followEvents.occurredAt))
  }

  async findInRange(broadcasterId: string, from: Date, to: Date): Promise<FollowEvent[]> {
    return db.select().from(followEvents)
      .where(and(eq(followEvents.broadcasterId, broadcasterId), gte(followEvents.occurredAt, from), lte(followEvents.occurredAt, to)))
      .orderBy(desc(followEvents.occurredAt))
  }
}

export const followEventsRepository = new FollowEventsRepository()