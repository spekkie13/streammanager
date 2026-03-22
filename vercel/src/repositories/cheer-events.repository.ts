import { db } from "@/lib/db"
import { cheerEvents } from "@/lib/schema"
import type { InsertCheerEvent } from "@/types/entities"

class CheerEventsRepository {
  async insert(data: InsertCheerEvent): Promise<void> {
    await db.insert(cheerEvents).values(data).onConflictDoNothing()
  }
}

export const cheerEventsRepository = new CheerEventsRepository()