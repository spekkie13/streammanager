import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { redirect } from "next/navigation"
import { linkedAccountsRepository, goalsRepository } from "@/repositories"
import { liveEventFeedService } from "@/services"
import { db } from "@/lib/db"
import { subGoals, ytMemberEvents, followEvents } from "@/lib/schema"
import { eq, count } from "drizzle-orm"
import { LiveClient } from "./live-client"

export type StreamInfo = {
  isLive: boolean
  title: string | null
  category: string | null
  viewerCount: number | null
  startedAt: string | null
}

async function fetchStreamInfo(broadcasterId: string, accessToken: string): Promise<StreamInfo> {
  try {
    const res = await fetch(
      `https://api.twitch.tv/helix/streams?user_id=${broadcasterId}`,
      { headers: { Authorization: `Bearer ${accessToken}`, "Client-Id": process.env.TWITCH_CLIENT_ID! } },
    )
    if (!res.ok) return { isLive: false, title: null, category: null, viewerCount: null, startedAt: null }
    const data = await res.json()
    const stream = data.data?.[0]
    if (!stream) return { isLive: false, title: null, category: null, viewerCount: null, startedAt: null }
    return {
      isLive: true,
      title: stream.title ?? null,
      category: stream.game_name ?? null,
      viewerCount: stream.viewer_count ?? null,
      startedAt: stream.started_at ?? null,
    }
  } catch {
    return { isLive: false, title: null, category: null, viewerCount: null, startedAt: null }
  }
}

export default async function LivePage() {
  const session = await getServerSession(authOptions)
  if (!session) redirect("/")

  const broadcasterId = session.twitchId ?? ""
  const youtubeChannelId = session.youtubeChannelId ?? null

  const linkedAccounts = await linkedAccountsRepository.findByUserId(session.userId)
  const twitchAccount = linkedAccounts.find(a => a.provider === "twitch")
  const ytAccount = linkedAccounts.find(a => a.provider === "youtube")

  const [streamInfo, recentEvents, goalRows, extraGoals, followTotalRows, ytMemberTotalRows] = await Promise.all([
    twitchAccount?.accessToken ? fetchStreamInfo(broadcasterId, twitchAccount.accessToken) : Promise.resolve<StreamInfo>({ isLive: false, title: null, category: null, viewerCount: null, startedAt: null }),
    liveEventFeedService.getFilteredEvents({ broadcasterId, youtubeChannelId, limit: 20 }),
    broadcasterId ? db.select().from(subGoals).where(eq(subGoals.broadcasterId, broadcasterId)).limit(1) : [],
    goalsRepository.findByUserId(session.userId),
    broadcasterId ? db.select({ total: count() }).from(followEvents).where(eq(followEvents.broadcasterId, broadcasterId)) : [{ total: 0 }],
    youtubeChannelId ? db.select({ total: count() }).from(ytMemberEvents).where(eq(ytMemberEvents.channelId, youtubeChannelId)) : [{ total: 0 }],
  ])

  const subGoalRow = goalRows[0] ?? null
  const followGoalRow = extraGoals.find(g => g.type === "twitch_follow") ?? null
  const ytMemberGoalRow = extraGoals.find(g => g.type === "youtube_member") ?? null

  return (
    <LiveClient
      displayName={session.displayName}
      hasYouTube={!!ytAccount}
      streamInfo={streamInfo}
      initialEvents={recentEvents.events}
      subGoal={subGoalRow ? { goal: subGoalRow.goal, initialCount: subGoalRow.initialCount, endsAt: subGoalRow.endsAt?.toISOString() ?? null } : null}
      followGoal={followGoalRow ? { goal: followGoalRow.goal } : null}
      followTotal={followTotalRows[0]?.total ?? 0}
      ytMemberGoal={ytMemberGoalRow ? { goal: ytMemberGoalRow.goal } : null}
      ytMemberTotal={ytMemberTotalRows[0]?.total ?? 0}
    />
  )
}
