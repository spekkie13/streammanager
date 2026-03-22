import { NextRequest, NextResponse } from "next/server"
import { createHmac, timingSafeEqual } from "crypto"
import { db } from "@/lib/db"
import { subEvents, streamSessions } from "@/lib/schema"
import { eq, isNull } from "drizzle-orm"

const TWITCH_MESSAGE_ID = "twitch-eventsub-message-id"
const TWITCH_MESSAGE_TIMESTAMP = "twitch-eventsub-message-timestamp"
const TWITCH_MESSAGE_SIGNATURE = "twitch-eventsub-message-signature"
const MESSAGE_TYPE = "twitch-eventsub-message-type"

function verifySignature(messageId: string, timestamp: string, body: string, signature: string): boolean {
  const secret = process.env.TWITCH_WEBHOOK_SECRET!
  const hmacMessage = messageId + timestamp + body
  const expected = "sha256=" + createHmac("sha256", secret).update(hmacMessage).digest("hex")
  try {
    return timingSafeEqual(Buffer.from(expected), Buffer.from(signature))
  } catch {
    return false
  }
}

export async function POST(req: NextRequest) {
  const body = await req.text()
  const messageId = req.headers.get(TWITCH_MESSAGE_ID) ?? ""
  const timestamp = req.headers.get(TWITCH_MESSAGE_TIMESTAMP) ?? ""
  const signature = req.headers.get(TWITCH_MESSAGE_SIGNATURE) ?? ""
  const messageType = req.headers.get(MESSAGE_TYPE) ?? ""

  if (!verifySignature(messageId, timestamp, body, signature)) {
    return NextResponse.json({ error: "Invalid signature" }, { status: 403 })
  }

  const payload = JSON.parse(body)

  if (messageType === "webhook_callback_verification") {
    return new NextResponse(payload.challenge, {
      status: 200,
      headers: { "Content-Type": "text/plain" },
    })
  }

  if (messageType === "notification") {
    const { subscription, event } = payload
    const broadcasterId: string = subscription.condition.broadcaster_user_id
    const occurredAt = new Date(timestamp)

    try {
      if (subscription.type === "channel.subscribe" && !event.is_gift) {
        await db.insert(subEvents).values({
          broadcasterId,
          eventId: messageId,
          userId: event.user_id,
          userLogin: event.user_login,
          userDisplayName: event.user_name,
          tier: event.tier,
          kind: "new",
          giftCount: 1,
          occurredAt,
        }).onConflictDoNothing()
      }

      if (subscription.type === "channel.subscription.message") {
        await db.insert(subEvents).values({
          broadcasterId,
          eventId: messageId,
          userId: event.user_id,
          userLogin: event.user_login,
          userDisplayName: event.user_name,
          tier: event.tier,
          kind: "resub",
          cumulativeMonths: event.cumulative_months ?? null,
          message: event.message?.text ?? null,
          giftCount: 1,
          occurredAt,
        }).onConflictDoNothing()
      }

      if (subscription.type === "channel.stream.online") {
        const existing = await db.select({ id: streamSessions.id })
          .from(streamSessions)
          .where(eq(streamSessions.broadcasterId, broadcasterId) && isNull(streamSessions.endedAt))
          .limit(1)
        if (existing.length === 0) {
          await db.insert(streamSessions).values({ broadcasterId, startedAt: occurredAt })
        }
      }

      if (subscription.type === "channel.stream.offline") {
        await db.update(streamSessions)
          .set({ endedAt: occurredAt })
          .where(eq(streamSessions.broadcasterId, broadcasterId) && isNull(streamSessions.endedAt))
      }

      if (subscription.type === "channel.subscription.gift") {
        await db.insert(subEvents).values({
          broadcasterId,
          eventId: messageId,
          gifterId: event.is_anonymous ? null : event.user_id,
          gifterLogin: event.is_anonymous ? null : event.user_login,
          gifterDisplayName: event.is_anonymous ? null : event.user_name,
          tier: event.tier,
          kind: "community_gift",
          giftCount: event.total ?? 1,
          occurredAt,
        }).onConflictDoNothing()
      }
    } catch (err) {
      console.error("DB insert error:", err)
    }
  }

  return NextResponse.json({ ok: true })
}
