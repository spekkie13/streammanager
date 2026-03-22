import { and, eq, desc, gt, gte, lte, sql } from "drizzle-orm"
import { db } from "@/lib/db"
import { subEvents } from "@/lib/schema"
import type { SubEvent, InsertSubEvent } from "@/types/entities"

class SubEventsRepository {
  async insert(data: InsertSubEvent): Promise<void> {
    await db.insert(subEvents).values(data).onConflictDoNothing()
  }

  async findByBroadcasterId(broadcasterId: string, since?: Date): Promise<SubEvent[]> {
    const condition = since
      ? and(eq(subEvents.broadcasterId, broadcasterId), gt(subEvents.occurredAt, since))
      : eq(subEvents.broadcasterId, broadcasterId)

    return db.select().from(subEvents).where(condition).orderBy(desc(subEvents.occurredAt))
  }

  async findSince(broadcasterId: string, since: Date): Promise<SubEvent[]> {
    return db.select().from(subEvents)
      .where(and(eq(subEvents.broadcasterId, broadcasterId), gt(subEvents.occurredAt, since)))
      .orderBy(desc(subEvents.occurredAt))
  }

  async findInRange(broadcasterId: string, from: Date, to: Date): Promise<SubEvent[]> {
    return db.select().from(subEvents)
      .where(and(eq(subEvents.broadcasterId, broadcasterId), gte(subEvents.occurredAt, from), lte(subEvents.occurredAt, to)))
      .orderBy(desc(subEvents.occurredAt))
  }

  async countByBroadcasterId(broadcasterId: string): Promise<number> {
    const result = await db.select({ count: sql<number>`count(*)` })
      .from(subEvents)
      .where(eq(subEvents.broadcasterId, broadcasterId))
    return Number(result[0].count)
  }
}

export const subEventsRepository = new SubEventsRepository()