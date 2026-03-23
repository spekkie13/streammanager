import { and, desc, eq, gt, gte, lte } from "drizzle-orm"
import { db } from "@/lib/db"
import { ytMemberEvents } from "@/lib/schema"
import type { YtMemberEvent, InsertYtMemberEvent } from "@/types/entities"

class YtMemberEventsRepository {
  async insert(data: InsertYtMemberEvent): Promise<void> {
    await db.insert(ytMemberEvents).values(data).onConflictDoNothing()
  }

  async findSince(channelId: string, since: Date): Promise<YtMemberEvent[]> {
    return db.select().from(ytMemberEvents)
      .where(and(eq(ytMemberEvents.channelId, channelId), gt(ytMemberEvents.occurredAt, since)))
      .orderBy(desc(ytMemberEvents.occurredAt))
  }

  async findInRange(channelId: string, from: Date, to: Date): Promise<YtMemberEvent[]> {
    return db.select().from(ytMemberEvents)
      .where(and(eq(ytMemberEvents.channelId, channelId), gte(ytMemberEvents.occurredAt, from), lte(ytMemberEvents.occurredAt, to)))
      .orderBy(desc(ytMemberEvents.occurredAt))
  }
}

export const ytMemberEventsRepository = new YtMemberEventsRepository()
