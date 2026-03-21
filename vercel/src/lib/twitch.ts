let appAccessToken: string | null = null
let tokenExpiry = 0

async function getAppAccessToken(): Promise<string> {
  if (appAccessToken && Date.now() < tokenExpiry) return appAccessToken

  const res = await fetch(
    `https://id.twitch.tv/oauth2/token?client_id=${process.env.TWITCH_CLIENT_ID}&client_secret=${process.env.TWITCH_CLIENT_SECRET}&grant_type=client_credentials`,
    { method: "POST" }
  )
  const data = await res.json()
  appAccessToken = data.access_token
  tokenExpiry = Date.now() + (data.expires_in - 60) * 1000
  return appAccessToken!
}

const SUB_TYPES = [
  { type: "channel.subscribe", version: "1" },
  { type: "channel.subscription.message", version: "1" },
  { type: "channel.subscription.gift", version: "1" },
]

export async function registerEventSubSubscriptions(broadcasterId: string): Promise<{ id: string; type: string; status: string }[]> {
  const token = await getAppAccessToken()
  const callbackUrl = `${process.env.NEXT_PUBLIC_APP_URL}/api/webhook`

  const results = []
  for (const { type, version } of SUB_TYPES) {
    const res = await fetch("https://api.twitch.tv/helix/eventsub/subscriptions", {
      method: "POST",
      headers: {
        "Authorization": `Bearer ${token}`,
        "Client-Id": process.env.TWITCH_CLIENT_ID!,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        type,
        version,
        condition: { broadcaster_user_id: broadcasterId },
        transport: {
          method: "webhook",
          callback: callbackUrl,
          secret: process.env.TWITCH_WEBHOOK_SECRET,
        },
      }),
    })

    const data = await res.json()
    if (res.ok && data.data?.[0]) {
      results.push({ id: data.data[0].id, type, status: data.data[0].status })
    } else if (res.status === 409) {
      // Already exists — that's fine
      results.push({ id: "existing", type, status: "enabled" })
    }
  }
  return results
}
