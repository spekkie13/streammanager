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

  // Attempt to register channel.chat.message and capture the raw response
  const APP_URL = (process.env.NEXT_PUBLIC_APP_URL ?? "").replace(/\/$/, "")
  const registerRes = await fetch("https://api.twitch.tv/helix/eventsub/subscriptions", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${twitchAccount.accessToken}`,
      "Client-Id": env.twitchClientId,
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      type: "channel.chat.message",
      version: "1",
      condition: { broadcaster_user_id: session.twitchId, user_id: session.twitchId },
      transport: {
        method: "webhook",
        callback: `${APP_URL}/api/webhook`,
        secret: process.env.TWITCH_WEBHOOK_SECRET,
      },
    }),
  })
  const registerData = await registerRes.json()

  return Response.json({
    tokenScopes: tokenInfo?.scopes ?? null,
    hasUserReadChat: tokenInfo?.scopes?.includes("user:read:chat") ?? false,
    chatMessageSubRegistered: !!chatSub,
    chatMessageSub: chatSub ?? null,
    registrationAttempt: { status: registerRes.status, body: registerData },
  })
}