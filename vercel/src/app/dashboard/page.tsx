import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { redirect } from "next/navigation"
import { db } from "@/lib/db"
import { subEvents, subGoals, followEvents, ytMemberEvents } from "@/lib/schema"
import { eq, count, and, gt, ne } from "drizzle-orm"
import { liveEventFeedService } from "@/services"
import { eventSubSubscriptionsRepository, userRepository, linkedAccountsRepository, goalsRepository } from "@/repositories"
import { DashboardClient } from "./dashboard-client"

async function fetchTwitchFollowerCount(broadcasterId: string, accessToken: string): Promise<number | null> {
  try {
    const res = await fetch(
      `https://api.twitch.tv/helix/channels/followers?broadcaster_id=${broadcasterId}&first=1`,
      { headers: { Authorization: `Bearer ${accessToken}`, "Client-Id": process.env.TWITCH_CLIENT_ID! } },
    )
    if (!res.ok) return null
    const data = await res.json()
    return data.total ?? null
  } catch { return null }
}

async function fetchTwitchSubCount(broadcasterId: string, accessToken: string): Promise<number | null> {
  try {
    const res = await fetch(
      `https://api.twitch.tv/helix/subscriptions?broadcaster_id=${broadcasterId}&first=1`,
      { headers: { Authorization: `Bearer ${accessToken}`, "Client-Id": process.env.TWITCH_CLIENT_ID! } },
    )
    if (!res.ok) return null
    const data = await res.json()
    return data.total ?? null
  } catch { return null }
}

async function refreshYouTubeToken(refreshToken: string): Promise<string | null> {
  try {
    const res = await fetch("https://oauth2.googleapis.com/token", {
      method: "POST",
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
      body: new URLSearchParams({
        grant_type: "refresh_token",
        refresh_token: refreshToken,
        client_id: process.env.GOOGLE_CLIENT_ID!,
        client_secret: process.env.GOOGLE_CLIENT_SECRET!,
      }),
    })
    const data = await res.json()
    return data.access_token ?? null
  } catch { return null }
}

async function fetchYouTubeSubCount(
  accessToken: string,
  refreshToken: string | null,
  channelId: string,
): Promise<number | null> {
  const doFetch = (token: string) =>
    fetch("https://www.googleapis.com/youtube/v3/channels?part=statistics&mine=true", {
      headers: { Authorization: `Bearer ${token}` },
    })

  try {
    let res = await doFetch(accessToken)

    if (res.status === 401 && refreshToken) {
      const newToken = await refreshYouTubeToken(refreshToken)
      if (!newToken) return null
      await linkedAccountsRepository.updateAccessToken("youtube", channelId, newToken)
      res = await doFetch(newToken)
    }

    if (!res.ok) return null
    const data = await res.json()
    const raw = data.items?.[0]?.statistics?.subscriberCount
    return raw !== undefined ? parseInt(raw) : null
  } catch { return null }
}

export default async function DashboardPage() {
  const session = await getServerSession(authOptions)
  if (!session) redirect("/")

  const broadcasterId = session.twitchId ?? ""
  const youtubeChannelId = session.youtubeChannelId ?? null
  const thirtyDaysAgo = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000)

  const [user, goalRows, totalRows, recentEvents, subscriptionsRegistered, linkedAccounts, followTotalRows, ytMemberTotalRows, extraGoals] = await Promise.all([
    userRepository.findById(session.userId),
    broadcasterId ? db.select().from(subGoals).where(eq(subGoals.broadcasterId, broadcasterId)).limit(1) : [],
    broadcasterId ? db.select({ total: count() }).from(subEvents).where(eq(subEvents.broadcasterId, broadcasterId)) : [{ total: 0 }],
    liveEventFeedService.getFilteredEvents({ broadcasterId, youtubeChannelId: session.youtubeChannelId, limit: 15 }),
    broadcasterId ? eventSubSubscriptionsRepository.existsByBroadcasterId(broadcasterId) : false,
    linkedAccountsRepository.findByUserId(session.userId),
    broadcasterId ? db.select({ total: count() }).from(followEvents).where(eq(followEvents.broadcasterId, broadcasterId)) : [{ total: 0 }],
    youtubeChannelId ? db.select({ total: count() }).from(ytMemberEvents).where(eq(ytMemberEvents.channelId, youtubeChannelId)) : [{ total: 0 }],
    goalsRepository.findByUserId(session.userId),
  ])

  if (!user?.onboardingCompleted) redirect("/setup")

  const twitchAccount = linkedAccounts.find(a => a.provider === "twitch")
  const ytAccount = linkedAccounts.find(a => a.provider === "youtube")

  const [followerCount, subCount, ytSubCount, followerGrowthRows, subGrowthRows] = await Promise.all([
    twitchAccount?.accessToken ? fetchTwitchFollowerCount(broadcasterId, twitchAccount.accessToken) : null,
    twitchAccount?.accessToken ? fetchTwitchSubCount(broadcasterId, twitchAccount.accessToken) : null,
    ytAccount?.accessToken ? fetchYouTubeSubCount(ytAccount.accessToken, ytAccount.refreshToken ?? null, ytAccount.providerAccountId) : null,
    broadcasterId
      ? db.select({ total: count() }).from(followEvents).where(and(eq(followEvents.broadcasterId, broadcasterId), gt(followEvents.occurredAt, thirtyDaysAgo)))
      : Promise.resolve([{ total: 0 }]),
    broadcasterId
      ? db.select({ total: count() }).from(subEvents).where(and(eq(subEvents.broadcasterId, broadcasterId), gt(subEvents.occurredAt, thirtyDaysAgo), ne(subEvents.kind, "resub")))
      : Promise.resolve([{ total: 0 }]),
  ])

  const goal = goalRows[0]?.goal ?? 100
  const initialCount = goalRows[0]?.initialCount ?? 0
  const endsAt = goalRows[0]?.endsAt?.toISOString() ?? null
  const total = totalRows[0]?.total ?? 0

  const followGoalRow = extraGoals.find(g => g.type === "twitch_follow") ?? null
  const ytMemberGoalRow = extraGoals.find(g => g.type === "youtube_member") ?? null

  return (
    <DashboardClient
      session={session}
      goal={goal}
      initialCount={initialCount}
      endsAt={endsAt}
      total={total}
      initialEvents={recentEvents.events}
      subscriptionsRegistered={subscriptionsRegistered}
      followerCount={followerCount}
      subCount={subCount}
      ytSubCount={ytSubCount}
      hasYouTube={!!ytAccount}
      followerGrowth={followerGrowthRows[0]?.total ?? 0}
      subGrowth={subGrowthRows[0]?.total ?? 0}
      followTotal={followTotalRows[0]?.total ?? 0}
      followGoal={followGoalRow?.goal ?? null}
      ytMemberTotal={ytMemberTotalRows[0]?.total ?? 0}
      ytMemberGoal={ytMemberGoalRow?.goal ?? null}
    />
  )
}
