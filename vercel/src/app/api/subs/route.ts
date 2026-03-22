import { NextRequest, NextResponse } from "next/server"
import { userRepository } from "@/repositories/user.repository"
import { subEventsRepository } from "@/repositories/sub-events.repository"

export async function GET(req: NextRequest) {
  const apiKey = req.headers.get("x-api-key") ?? req.nextUrl.searchParams.get("key") ?? ""
  const since = req.nextUrl.searchParams.get("since")

  if (!apiKey) return NextResponse.json({ error: "Missing API key" }, { status: 401 })

  const user = await userRepository.findByApiKey(apiKey)
  if (!user) return NextResponse.json({ error: "Invalid API key" }, { status: 401 })

  const events = await subEventsRepository.findByBroadcasterId(user.twitchId, since ? new Date(since) : undefined)
  return NextResponse.json({ events })
}
