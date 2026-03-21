import { NextRequest, NextResponse } from "next/server"
import { db } from "@/lib/db"
import { waitlist } from "@/lib/schema"

export async function POST(req: NextRequest) {
  const { email, twitchLogin } = await req.json()

  if (!email || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
    return NextResponse.json({ error: "Valid email required" }, { status: 400 })
  }

  try {
    await db.insert(waitlist).values({
      email: email.toLowerCase().trim(),
      twitchLogin: twitchLogin?.trim() || null,
    }).onConflictDoNothing()

    return NextResponse.json({ ok: true })
  } catch {
    return NextResponse.json({ error: "Something went wrong" }, { status: 500 })
  }
}