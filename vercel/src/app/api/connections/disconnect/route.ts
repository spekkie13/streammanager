import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { linkedAccountsRepository } from "@/repositories"
import { and, eq } from "drizzle-orm"
import { db } from "@/lib/db"
import { linkedAccounts } from "@/lib/schema"

export async function POST(req: Request) {
  const session = await getServerSession(authOptions)
  if (!session?.userId) return new Response("Unauthorized", { status: 401 })

  const { provider } = await req.json() as { provider: string }
  if (!provider || !["youtube", "twitch"].includes(provider)) {
    return new Response("Invalid provider", { status: 400 })
  }

  // Prevent disconnecting the only linked account — user would be locked out
  const allAccounts = await linkedAccountsRepository.findByUserId(session.userId)
  if (allAccounts.length <= 1) {
    return new Response("Cannot disconnect your only linked account", { status: 400 })
  }

  await db.delete(linkedAccounts).where(
    and(
      eq(linkedAccounts.userId, session.userId),
      eq(linkedAccounts.provider, provider),
    )
  )

  return new Response(null, { status: 204 })
}
