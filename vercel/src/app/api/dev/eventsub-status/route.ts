import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { linkedAccountsRepository } from "@/repositories"
import { env } from "@/lib/env"

export async function GET() {
  const session = await getServerSession(authOptions)
  if (!session?.userId || !session?.twitchId) return new Response("Unauthorized", { status: 401 })

  const accounts = await linkedAccountsRepository.findByUserId(session.userId)
  const twitchAccount = accounts.find(a => a.provider === "twitch")
  if (!twitchAccount?.accessToken) return Response.json({ error: "No Twitch token" })

  // Check scopes on the stored user token
  const validateRes = await fetch("https://id.twitch.tv/oauth2/validate", {
    headers: { Authorization: `OAuth ${twitchAccount.accessToken}` },
  })
  const tokenInfo = validateRes.ok ? await validateRes.json() : null

  // List active EventSub subscriptions from Twitch
  const subsRes = await fetch("https://api.twitch.tv/helix/eventsub/subscriptions?status=enabled", {
    headers: {
      Authorization: `Bearer ${twitchAccount.accessToken}`,
      "Client-Id": env.twitchClientId,
    },
  })
  const subsData = subsRes.ok ? await subsRes.json() : null

  const chatSub = subsData?.data?.find((s: { type: string }) => s.type === "channel.chat.message")

  return Response.json({
    tokenScopes: tokenInfo?.scopes ?? null,
    hasUserReadChat: tokenInfo?.scopes?.includes("user:read:chat") ?? false,
    chatMessageSubRegistered: !!chatSub,
    chatMessageSub: chatSub ?? null,
  })
}