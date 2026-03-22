import { db } from "@/lib/db"
import { waitlist } from "@/lib/schema"
import type { WaitlistEntry } from "@/types/entities"

class WaitlistRepository {
  async insert(email: string, twitchLogin?: string): Promise<void> {
    await db.insert(waitlist).values({ email, twitchLogin: twitchLogin ?? null }).onConflictDoNothing()
  }
}

export const waitlistRepository = new WaitlistRepository()