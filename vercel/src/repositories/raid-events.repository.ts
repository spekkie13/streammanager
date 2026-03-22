import { db } from "@/lib/db"
import { raidEvents } from "@/lib/schema"
import type { InsertRaidEvent } from "@/types/entities"

class RaidEventsRepository {
  async insert(data: InsertRaidEvent): Promise<void> {
    await db.insert(raidEvents).values(data).onConflictDoNothing()
  }
}

export const raidEventsRepository = new RaidEventsRepository()