import { and, eq, isNull } from "drizzle-orm"
import { db } from "@/lib/db"
import { streamSessions } from "@/lib/schema"

class StreamSessionRepository {
  async findOpen(broadcasterId: string) {
    const rows = await db.select().from(streamSessions)
      .where(and(eq(streamSessions.broadcasterId, broadcasterId), isNull(streamSessions.endedAt)))
      .limit(1)
    return rows[0] ?? null
  }

  async create(broadcasterId: string, startedAt: Date) {
    await db.insert(streamSessions).values({ broadcasterId, startedAt })
  }

  async close(broadcasterId: string, endedAt: Date) {
    await db.update(streamSessions)
      .set({ endedAt })
      .where(and(eq(streamSessions.broadcasterId, broadcasterId), isNull(streamSessions.endedAt)))
  }
}

export const streamSessionRepository = new StreamSessionRepository()
