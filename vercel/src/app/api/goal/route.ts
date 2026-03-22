import { NextRequest, NextResponse } from "next/server"
import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { subGoalsRepository } from "@/repositories"

export async function GET() {
  const session = await getServerSession(authOptions)
  if (!session) return NextResponse.json({ error: "Unauthorized" }, { status: 401 })

  const row = await subGoalsRepository.findByBroadcasterId(session.twitchId)
  return NextResponse.json({ goal: row?.goal ?? 100, endsAt: row?.endsAt?.toISOString() ?? null })
}

export async function POST(req: NextRequest) {
  const session = await getServerSession(authOptions)
  if (!session) return NextResponse.json({ error: "Unauthorized" }, { status: 401 })

  const { goal, endsAt } = await req.json()
  if (typeof goal !== "number" || goal < 1) return NextResponse.json({ error: "Invalid goal" }, { status: 400 })

  const endsAtDate = endsAt ? new Date(endsAt) : null

  await subGoalsRepository.upsert(session.twitchId, goal, endsAtDate)
  return NextResponse.json({ ok: true, goal, endsAt: endsAtDate?.toISOString() ?? null })
}
