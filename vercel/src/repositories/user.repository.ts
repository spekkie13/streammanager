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
}

export const userRepository = new UserRepository()
