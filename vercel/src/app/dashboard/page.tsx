import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { redirect } from "next/navigation"
import { db } from "@/lib/db"
import { subEvents, subGoals } from "@/lib/schema"
import { eq, count } from "drizzle-orm"
import { liveEventFeedService } from "@/services"
import { DashboardClient } from "./dashboard-client"

export default async function DashboardPage() {
  const session = await getServerSession(authOptions)
  if (!session) redirect("/")

  const [goalRows, totalRows, recentEvents] = await Promise.all([
    db.select().from(subGoals).where(eq(subGoals.broadcasterId, session.twitchId)).limit(1),
    db.select({ total: count() }).from(subEvents).where(eq(subEvents.broadcasterId, session.twitchId)),
    liveEventFeedService.getFilteredEvents({ broadcasterId: session.twitchId, limit: 50 }),
  ])

  const goal = goalRows[0]?.goal ?? 100
  const total = totalRows[0]?.total ?? 0
  const webhookUrl = `${process.env.NEXT_PUBLIC_APP_URL}/api/webhook`

  return (
    <DashboardClient
      session={session}
      goal={goal}
      total={total}
      webhookUrl={webhookUrl}
      initialEvents={recentEvents.events}
    />
  )
}
