import { eq } from "drizzle-orm"
import { db } from "@/lib/db"
import { subGoals } from "@/lib/schema"
import type { SubGoal } from "@/types/entities"

class SubGoalsRepository {
  async findByBroadcasterId(broadcasterId: string): Promise<SubGoal | null> {
    const rows = await db.select().from(subGoals).where(eq(subGoals.broadcasterId, broadcasterId)).limit(1)
    return rows[0] ?? null
  }

  async upsert(broadcasterId: string, goal: number): Promise<void> {
    await db.insert(subGoals)
      .values({ broadcasterId, goal })
      .onConflictDoUpdate({ target: subGoals.broadcasterId, set: { goal, updatedAt: new Date() } })
  }
}

export const subGoalsRepository = new SubGoalsRepository()