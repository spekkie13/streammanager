import { NextResponse } from "next/server"
import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { twitchEventSubService } from "@/services"
import { eventSubSubscriptionsRepository, linkedAccountsRepository } from "@/repositories"

export async function POST() {
  const session = await getServerSession(authOptions)
  if (!session?.twitchId || !session?.userId) return NextResponse.json({ error: "Unauthorized" }, { status: 401 })

  const accounts = await linkedAccountsRepository.findByUserId(session.userId)
  const twitchAccount = accounts.find(a => a.provider === "twitch")
  const userAccessToken = twitchAccount?.accessToken ?? undefined

  const results = await twitchEventSubService.registerSubscriptions(session.twitchId, userAccessToken)

  for (const sub of results) {
    await eventSubSubscriptionsRepository.insert({ id: sub.id, broadcasterId: session.twitchId, type: sub.type, status: sub.status })
  }

  return NextResponse.json({ subscriptions: results })
}
