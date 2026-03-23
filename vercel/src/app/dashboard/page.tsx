import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { redirect } from "next/navigation"
import { db } from "@/lib/db"
import { subEvents, subGoals } from "@/lib/schema"
import { eq, count } from "drizzle-orm"
import { liveEventFeedService } from "@/services"
import { eventSubSubscriptionsRepository } from "@/repositories"
import { DashboardClient } from "./dashboard-client"

export default async function DashboardPage() {
  const session = await getServerSession(authOptions)
  if (!session) redirect("/")

  const broadcasterId = session.twitchId ?? ""

  const [goalRows, totalRows, recentEvents, subscriptionsRegistered] = await Promise.all([
    broadcasterId ? db.select().from(subGoals).where(eq(subGoals.broadcasterId, broadcasterId)).limit(1) : [],
    broadcasterId ? db.select({ total: count() }).from(subEvents).where(eq(subEvents.broadcasterId, broadcasterId)) : [{ total: 0 }],
    liveEventFeedService.getFilteredEvents({ broadcasterId, youtubeChannelId: session.youtubeChannelId, limit: 50 }),
    broadcasterId ? eventSubSubscriptionsRepository.existsByBroadcasterId(broadcasterId) : false,
  ])

  const goal = goalRows[0]?.goal ?? 100
  const initialCount = goalRows[0]?.initialCount ?? 0
  const endsAt = goalRows[0]?.endsAt?.toISOString() ?? null
  const total = totalRows[0]?.total ?? 0

  return (
    <DashboardClient
      session={session}
      goal={goal}
      initialCount={initialCount}
      endsAt={endsAt}
      total={total}
      initialEvents={recentEvents.events}
      subscriptionsRegistered={subscriptionsRegistered}
    />
  )
}
