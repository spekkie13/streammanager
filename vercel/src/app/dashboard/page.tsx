import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { redirect } from "next/navigation"
import { db } from "@/lib/db"
import { subEvents, subGoals } from "@/lib/schema"
import { eq, desc, count } from "drizzle-orm"
import { DashboardClient } from "./dashboard-client"

export default async function DashboardPage() {
  const session = await getServerSession(authOptions)
  if (!session) redirect("/")

  const goalRows = await db.select().from(subGoals).where(eq(subGoals.broadcasterId, session.twitchId)).limit(1)
  const goal = goalRows[0]?.goal ?? 100

  const recentSubs = await db.select().from(subEvents)
    .where(eq(subEvents.broadcasterId, session.twitchId))
    .orderBy(desc(subEvents.occurredAt))
    .limit(50)

  const totalRows = await db.select({ total: count() }).from(subEvents)
    .where(eq(subEvents.broadcasterId, session.twitchId))
  const total = totalRows[0]?.total ?? 0

  const webhookUrl = `${process.env.NEXT_PUBLIC_APP_URL}/api/webhook`

  return (
    <DashboardClient
      session={session}
      goal={goal}
      total={total}
      recentSubs={recentSubs.map(s => ({
        ...s,
        occurredAt: s.occurredAt.toISOString(),
        createdAt: s.createdAt.toISOString(),
      }))}
      webhookUrl={webhookUrl}
    />
  )
}
