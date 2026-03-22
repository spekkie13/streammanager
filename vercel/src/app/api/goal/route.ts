import { NextRequest, NextResponse } from "next/server"
import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { subGoalsRepository } from "@/repositories/sub-goals.repository"

export async function GET() {
  const session = await getServerSession(authOptions)
  if (!session) return NextResponse.json({ error: "Unauthorized" }, { status: 401 })

  const row = await subGoalsRepository.findByBroadcasterId(session.twitchId)
  return NextResponse.json({ goal: row?.goal ?? 100 })
}

export async function POST(req: NextRequest) {
  const session = await getServerSession(authOptions)
  if (!session) return NextResponse.json({ error: "Unauthorized" }, { status: 401 })

  const { goal } = await req.json()
  if (typeof goal !== "number" || goal < 1) return NextResponse.json({ error: "Invalid goal" }, { status: 400 })

  await subGoalsRepository.upsert(session.twitchId, goal)
  return NextResponse.json({ ok: true, goal })
}
