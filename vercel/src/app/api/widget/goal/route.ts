import { NextRequest, NextResponse } from "next/server"
import { db } from "@/lib/db"
import { subEvents, subGoals, followEvents, ytMemberEvents } from "@/lib/schema"
import { eq, count } from "drizzle-orm"
import { userRepository, linkedAccountsRepository, goalsRepository } from "@/repositories"

export async function GET(req: NextRequest) {
  const { searchParams } = new URL(req.url)
  const token = searchParams.get("token")
  const type = searchParams.get("type") ?? "twitch_sub"

  if (!token) return NextResponse.json({ error: "Missing token" }, { status: 400 })

  const user = await userRepository.findByWidgetToken(token)
  if (!user) return NextResponse.json({ error: "Invalid token" }, { status: 401 })

  const linkedAccounts = await linkedAccountsRepository.findByUserId(user.id)
  const twitchAccount = linkedAccounts.find(a => a.provider === "twitch")
  const ytAccount = linkedAccounts.find(a => a.provider === "youtube")
  const broadcasterId = twitchAccount?.providerAccountId ?? ""
  const channelId = ytAccount?.providerAccountId ?? ""

  if (type === "twitch_sub") {
    const [goalRow, totalRow] = await Promise.all([
      broadcasterId ? db.select().from(subGoals).where(eq(subGoals.broadcasterId, broadcasterId)).limit(1) : [],
      broadcasterId ? db.select({ total: count() }).from(subEvents).where(eq(subEvents.broadcasterId, broadcasterId)) : [{ total: 0 }],
    ])
    const goal = goalRow[0]?.goal ?? 100
    const initialCount = goalRow[0]?.initialCount ?? 0
    const current = (totalRow[0]?.total ?? 0) + initialCount
    return NextResponse.json({ current, goal, label: "Subscribers", platform: "twitch" })
  }

  if (type === "twitch_follow") {
    const [goalRow, totalRow] = await Promise.all([
      goalsRepository.findByUserIdAndType(user.id, "twitch_follow"),
      broadcasterId ? db.select({ total: count() }).from(followEvents).where(eq(followEvents.broadcasterId, broadcasterId)) : [{ total: 0 }],
    ])
    const goal = goalRow?.goal ?? 100
    const current = totalRow[0]?.total ?? 0
    return NextResponse.json({ current, goal, label: "Followers", platform: "twitch" })
  }

  if (type === "youtube_member") {
    const [goalRow, totalRow] = await Promise.all([
      goalsRepository.findByUserIdAndType(user.id, "youtube_member"),
      channelId ? db.select({ total: count() }).from(ytMemberEvents).where(eq(ytMemberEvents.channelId, channelId)) : [{ total: 0 }],
    ])
    const goal = goalRow?.goal ?? 100
    const current = totalRow[0]?.total ?? 0
    return NextResponse.json({ current, goal, label: "Members", platform: "youtube" })
  }

  return NextResponse.json({ error: "Invalid type" }, { status: 400 })
}
