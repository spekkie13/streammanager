import { db } from "@/lib/db"
import { followEvents } from "@/lib/schema"
import type { InsertFollowEvent } from "@/types/entities"

class FollowEventsRepository {
  async insert(data: InsertFollowEvent): Promise<void> {
    await db.insert(followEvents).values(data).onConflictDoNothing()
  }
}

export const followEventsRepository = new FollowEventsRepository()