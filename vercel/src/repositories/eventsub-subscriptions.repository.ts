import { db } from "@/lib/db"
import { eventsubSubscriptions } from "@/lib/schema"

type InsertEventSubSubscription = typeof eventsubSubscriptions.$inferInsert

class EventSubSubscriptionsRepository {
  async insert(data: InsertEventSubSubscription) {
    await db.insert(eventsubSubscriptions).values(data).onConflictDoNothing()
  }
}

export const eventSubSubscriptionsRepository = new EventSubSubscriptionsRepository()
