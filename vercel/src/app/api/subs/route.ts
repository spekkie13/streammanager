import { NextRequest, NextResponse } from "next/server"
import { db } from "@/lib/db"
import { subEvents, users } from "@/lib/schema"
import { eq, gte, and, desc } from "drizzle-orm"

export async function GET(req: NextRequest) {
  const apiKey = req.headers.get("x-api-key") ?? req.nextUrl.searchParams.get("key") ?? ""
  const since = req.nextUrl.searchParams.get("since")

  if (!apiKey) return NextResponse.json({ error: "Missing API key" }, { status: 401 })

  const user = await db.select({ twitchId: users.twitchId }).from(users).where(eq(users.apiKey, apiKey)).limit(1)
  if (!user.length) return NextResponse.json({ error: "Invalid API key" }, { status: 401 })

  const broadcasterId = user[0].twitchId

  const condition = since
    ? and(eq(subEvents.broadcasterId, broadcasterId), gte(subEvents.occurredAt, new Date(since)))
    : eq(subEvents.broadcasterId, broadcasterId)

  const events = await db.select().from(subEvents)
    .where(condition)
    .orderBy(desc(subEvents.occurredAt))

  return NextResponse.json({ events })
}
