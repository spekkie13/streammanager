import { db } from "@/lib/db"
import { cheerEvents } from "@/lib/schema"

type InsertCheerEvent = typeof cheerEvents.$inferInsert

class CheerEventsRepository {
  async insert(data: InsertCheerEvent) {
    await db.insert(cheerEvents).values(data).onConflictDoNothing()
  }
}

export const cheerEventsRepository = new CheerEventsRepository()
