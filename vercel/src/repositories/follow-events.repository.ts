import { and, eq, gt } from "drizzle-orm"
import { db } from "@/lib/db"
import { followEvents } from "@/lib/schema"
import type { FollowEvent, InsertFollowEvent } from "@/types/entities"

class FollowEventsRepository {
  async insert(data: InsertFollowEvent): Promise<void> {
    await db.insert(followEvents).values(data).onConflictDoNothing()
  }

  async findSince(broadcasterId: string, since: Date): Promise<FollowEvent[]> {
    return db.select().from(followEvents)
      .where(and(eq(followEvents.broadcasterId, broadcasterId), gt(followEvents.occurredAt, since)))
  }
}

export const followEventsRepository = new FollowEventsRepository()