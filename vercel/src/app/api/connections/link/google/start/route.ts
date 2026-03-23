import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { env } from "@/lib/env"
import { NextResponse } from "next/server"
import { randomBytes } from "crypto"

export async function GET() {
  const session = await getServerSession(authOptions)
  if (!session?.userId) {
    return NextResponse.redirect(new URL("/", process.env.NEXTAUTH_URL!))
  }

  const state = randomBytes(16).toString("hex")

  const params = new URLSearchParams({
    client_id: env.googleClientId,
    redirect_uri: `${process.env.NEXTAUTH_URL}/api/connections/link/google/callback`,
    response_type: "code",
    scope: "openid email profile https://www.googleapis.com/auth/youtube.readonly",
    access_type: "offline",
    prompt: "consent",
    state,
  })

  const response = NextResponse.redirect(
    `https://accounts.google.com/o/oauth2/v2/auth?${params}`
  )

  // Store state + userId in a short-lived cookie for CSRF protection
  response.cookies.set("yt_link_state", JSON.stringify({ state, userId: session.userId }), {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    maxAge: 600,
    path: "/",
  })

  return response
}
