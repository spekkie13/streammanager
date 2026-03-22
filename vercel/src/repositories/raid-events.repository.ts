import { db } from "@/lib/db"
import { raidEvents } from "@/lib/schema"

type InsertRaidEvent = typeof raidEvents.$inferInsert

class RaidEventsRepository {
  async insert(data: InsertRaidEvent) {
    await db.insert(raidEvents).values(data).onConflictDoNothing()
  }
}

export const raidEventsRepository = new RaidEventsRepository()
