import { NextRequest, NextResponse } from "next/server"
import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { db } from "@/lib/db"
import { subGoals } from "@/lib/schema"
import { eq } from "drizzle-orm"

export async function GET(req: NextRequest) {
  const session = await getServerSession(authOptions)
  if (!session) return NextResponse.json({ error: "Unauthorized" }, { status: 401 })

  const rows = await db.select().from(subGoals).where(eq(subGoals.broadcasterId, session.twitchId)).limit(1)
  const goal = rows[0]?.goal ?? 100
  return NextResponse.json({ goal })
}

export async function POST(req: NextRequest) {
  const session = await getServerSession(authOptions)
  if (!session) return NextResponse.json({ error: "Unauthorized" }, { status: 401 })

  const { goal } = await req.json()
  if (typeof goal !== "number" || goal < 1) return NextResponse.json({ error: "Invalid goal" }, { status: 400 })

  await db.insert(subGoals)
    .values({ broadcasterId: session.twitchId, goal })
    .onConflictDoUpdate({ target: subGoals.broadcasterId, set: { goal, updatedAt: new Date() } })

  return NextResponse.json({ ok: true, goal })
}
