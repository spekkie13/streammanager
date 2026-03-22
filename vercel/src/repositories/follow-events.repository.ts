import { db } from "@/lib/db"
import { followEvents } from "@/lib/schema"

type InsertFollowEvent = typeof followEvents.$inferInsert

class FollowEventsRepository {
  async insert(data: InsertFollowEvent) {
    await db.insert(followEvents).values(data).onConflictDoNothing()
  }
}

export const followEventsRepository = new FollowEventsRepository()
