import { NextResponse } from "next/server"
import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { twitchEventSubService } from "@/services"
import { eventSubSubscriptionsRepository } from "@/repositories"

export async function POST() {
  const session = await getServerSession(authOptions)
  if (!session) return NextResponse.json({ error: "Unauthorized" }, { status: 401 })

  const results = await twitchEventSubService.registerSubscriptions(session.twitchId)

  for (const sub of results) {
    if (sub.id !== "existing") {
      await eventSubSubscriptionsRepository.insert({ id: sub.id, broadcasterId: session.twitchId, type: sub.type, status: sub.status })
    }
  }

  return NextResponse.json({ ok: true, subscriptions: results })
}
