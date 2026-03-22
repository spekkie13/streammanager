import { db } from "@/lib/db"
import { eventsubSubscriptions } from "@/lib/schema"
import type { InsertEventSubSubscription } from "@/types/entities"

class EventSubSubscriptionsRepository {
  async insert(data: InsertEventSubSubscription): Promise<void> {
    await db.insert(eventsubSubscriptions).values(data).onConflictDoNothing()
  }
}

export const eventSubSubscriptionsRepository = new EventSubSubscriptionsRepository()