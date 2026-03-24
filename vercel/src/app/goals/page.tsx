import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { redirect } from "next/navigation"
import { db } from "@/lib/db"
import { subEvents, subGoals } from "@/lib/schema"
import { eq, count } from "drizzle-orm"
import { AppHeader } from "@/components/app-header"
import { GoalsClient } from "./goals-client"

export default async function GoalsPage() {
  const session = await getServerSession(authOptions)
  if (!session) redirect("/")

  const broadcasterId = session.twitchId ?? ""

  const [goalRows, totalRows] = await Promise.all([
    broadcasterId ? db.select().from(subGoals).where(eq(subGoals.broadcasterId, broadcasterId)).limit(1) : [],
    broadcasterId ? db.select({ total: count() }).from(subEvents).where(eq(subEvents.broadcasterId, broadcasterId)) : [{ total: 0 }],
  ])

  const goal = goalRows[0]?.goal ?? 100
  const initialCount = goalRows[0]?.initialCount ?? 0
  const endsAt = goalRows[0]?.endsAt?.toISOString() ?? null
  const total = totalRows[0]?.total ?? 0

  return (
    <div className="min-h-screen">
      <AppHeader displayName={session.displayName} />
      <main className="max-w-3xl mx-auto px-6 py-10 space-y-6">
        <div>
          <h1 className="text-2xl font-bold">Goals</h1>
          <p className="text-zinc-500 text-sm mt-1">Track and manage your streaming goals.</p>
        </div>
        <GoalsClient
          goal={goal}
          initialCount={initialCount}
          endsAt={endsAt}
          total={total}
        />
      </main>
    </div>
  )
}
