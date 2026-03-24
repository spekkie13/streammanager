import { eq } from "drizzle-orm"
import { db } from "@/lib/db"
import { users } from "@/lib/schema"
import type { User } from "@/types/entities"

class UserRepository {
  async findById(id: string): Promise<User | null> {
    const rows = await db.select().from(users).where(eq(users.id, id)).limit(1)
    return rows[0] ?? null
  }

  async findByApiKey(apiKey: string): Promise<User | null> {
    const rows = await db.select().from(users).where(eq(users.apiKey, apiKey)).limit(1)
    return rows[0] ?? null
  }

  async findByWidgetToken(token: string): Promise<User | null> {
    const rows = await db.select().from(users).where(eq(users.widgetToken, token)).limit(1)
    return rows[0] ?? null
  }

  async setWidgetToken(userId: string, token: string): Promise<void> {
    await db.update(users).set({ widgetToken: token }).where(eq(users.id, userId))
  }

  async completeOnboarding(userId: string): Promise<void> {
    await db.update(users)
      .set({ onboardingCompleted: true })
      .where(eq(users.id, userId))
  }
}

export const userRepository = new UserRepository()
