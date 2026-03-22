import { and, eq, desc, gt } from "drizzle-orm"
import { db } from "@/lib/db"
import { subEvents } from "@/lib/schema"

type InsertSubEvent = typeof subEvents.$inferInsert

class SubEventsRepository {
  async insert(data: InsertSubEvent) {
    await db.insert(subEvents).values(data).onConflictDoNothing()
  }

  async findByBroadcasterId(broadcasterId: string, since?: Date) {
    const condition = since
      ? and(eq(subEvents.broadcasterId, broadcasterId), gt(subEvents.occurredAt, since))
      : eq(subEvents.broadcasterId, broadcasterId)

    return db.select().from(subEvents).where(condition).orderBy(desc(subEvents.occurredAt))
  }

  async countByBroadcasterId(broadcasterId: string) {
    const rows = await db.select().from(subEvents).where(eq(subEvents.broadcasterId, broadcasterId))
    return rows.length
  }
}

export const subEventsRepository = new SubEventsRepository()
