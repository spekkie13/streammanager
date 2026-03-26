import { NextRequest, NextResponse } from "next/server"
import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { analyticsService } from "@/services"

const RANGES = { "7d": 7, "30d": 30, "90d": 90 } as const
type Range = keyof typeof RANGES

export async function GET(req: NextRequest) {
  const session = await getServerSession(authOptions)
  if (!session) return NextResponse.json({ error: "Unauthorized" }, { status: 401 })

  const { searchParams } = new URL(req.url)
  const range = (searchParams.get("range") ?? "30d") as Range
  const days = RANGES[range] ?? 30

  const since = new Date(Date.now() - days * 24 * 60 * 60 * 1000)

  const data = await analyticsService.getOverview(
    session.twitchId ?? "",
    session.youtubeChannelId ?? null,
    since,
  )

  return NextResponse.json(data)
}