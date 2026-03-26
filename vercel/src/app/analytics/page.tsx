import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { redirect } from "next/navigation"
import { analyticsService } from "@/services"
import { AnalyticsClient } from "./analytics-client"

export default async function AnalyticsPage() {
  const session = await getServerSession(authOptions)
  if (!session) redirect("/")

  const since = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000)
  const data = await analyticsService.getOverview(
    session.twitchId ?? "",
    session.youtubeChannelId ?? null,
    since,
  )

  return (
    <AnalyticsClient
      initialData={data}
      initialRange="30d"
      hasYouTube={!!session.youtubeChannelId}
      displayName={session.displayName}
    />
  )
}