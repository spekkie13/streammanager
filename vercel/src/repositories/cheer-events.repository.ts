import { and, eq, gt } from "drizzle-orm"
import { db } from "@/lib/db"
import { cheerEvents } from "@/lib/schema"
import type { CheerEvent, InsertCheerEvent } from "@/types/entities"

class CheerEventsRepository {
  async insert(data: InsertCheerEvent): Promise<void> {
    await db.insert(cheerEvents).values(data).onConflictDoNothing()
  }

  async findSince(broadcasterId: string, since: Date): Promise<CheerEvent[]> {
    return db.select().from(cheerEvents)
      .where(and(eq(cheerEvents.broadcasterId, broadcasterId), gt(cheerEvents.occurredAt, since)))
  }
}

export const cheerEventsRepository = new CheerEventsRepository()