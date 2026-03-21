import { NextResponse } from "next/server"
import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { db } from "@/lib/db"
import { eventsubSubscriptions } from "@/lib/schema"
import { registerEventSubSubscriptions } from "@/lib/twitch"

export async function POST() {
  const session = await getServerSession(authOptions)
  if (!session) return NextResponse.json({ error: "Unauthorized" }, { status: 401 })

  const results = await registerEventSubSubscriptions(session.twitchId)

  for (const sub of results) {
    if (sub.id !== "existing") {
      await db.insert(eventsubSubscriptions)
        .values({ id: sub.id, broadcasterId: session.twitchId, type: sub.type, status: sub.status })
        .onConflictDoNothing()
    }
  }

  return NextResponse.json({ ok: true, subscriptions: results })
}
