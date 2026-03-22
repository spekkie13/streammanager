import { eq } from "drizzle-orm"
import { db } from "@/lib/db"
import { subGoals } from "@/lib/schema"

class SubGoalsRepository {
  async findByBroadcasterId(broadcasterId: string) {
    const rows = await db.select().from(subGoals).where(eq(subGoals.broadcasterId, broadcasterId)).limit(1)
    return rows[0] ?? null
  }

  async upsert(broadcasterId: string, goal: number) {
    await db.insert(subGoals)
      .values({ broadcasterId, goal })
      .onConflictDoUpdate({ target: subGoals.broadcasterId, set: { goal, updatedAt: new Date() } })
  }
}

export const subGoalsRepository = new SubGoalsRepository()
