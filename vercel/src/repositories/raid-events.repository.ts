import { and, eq, gt } from "drizzle-orm"
import { db } from "@/lib/db"
import { raidEvents } from "@/lib/schema"
import type { RaidEvent, InsertRaidEvent } from "@/types/entities"

class RaidEventsRepository {
  async insert(data: InsertRaidEvent): Promise<void> {
    await db.insert(raidEvents).values(data).onConflictDoNothing()
  }

  async findSince(broadcasterId: string, since: Date): Promise<RaidEvent[]> {
    return db.select().from(raidEvents)
      .where(and(eq(raidEvents.broadcasterId, broadcasterId), gt(raidEvents.occurredAt, since)))
  }
}

export const raidEventsRepository = new RaidEventsRepository()