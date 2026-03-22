import { eq } from "drizzle-orm"
import { db } from "@/lib/db"
import { users } from "@/lib/schema"
import { randomBytes } from "crypto"
import type { User } from "@/types/entities"

class UserRepository {
  async findByTwitchId(twitchId: string): Promise<User | null> {
    const rows = await db.select().from(users).where(eq(users.twitchId, twitchId)).limit(1)
    return rows[0] ?? null
  }

  async findByApiKey(apiKey: string): Promise<User | null> {
    const rows = await db.select().from(users).where(eq(users.apiKey, apiKey)).limit(1)
    return rows[0] ?? null
  }

  async upsert(twitchId: string, login: string, displayName: string, accessToken: string, refreshToken: string): Promise<string> {
    const existing = await this.findByTwitchId(twitchId)

    if (!existing) {
      const apiKey = randomBytes(32).toString("hex")
      await db.insert(users).values({ twitchId, twitchLogin: login, twitchDisplayName: displayName, accessToken, refreshToken, apiKey })
      return apiKey
    }

    await db.update(users)
      .set({ twitchLogin: login, twitchDisplayName: displayName, accessToken, refreshToken })
      .where(eq(users.twitchId, twitchId))
    return existing.apiKey
  }
}

export const userRepository = new UserRepository()