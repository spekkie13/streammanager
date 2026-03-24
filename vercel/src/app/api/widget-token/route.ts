import { NextResponse } from "next/server"
import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { userRepository } from "@/repositories"

export async function GET() {
  const session = await getServerSession(authOptions)
  if (!session) return NextResponse.json({ error: "Unauthorized" }, { status: 401 })

  const user = await userRepository.findById(session.userId)
  if (!user) return NextResponse.json({ error: "Not found" }, { status: 404 })

  // Generate on first request
  if (!user.widgetToken) {
    const token = crypto.randomUUID()
    await userRepository.setWidgetToken(session.userId, token)
    return NextResponse.json({ token })
  }

  return NextResponse.json({ token: user.widgetToken })
}

export async function POST() {
  const session = await getServerSession(authOptions)
  if (!session) return NextResponse.json({ error: "Unauthorized" }, { status: 401 })

  const token = crypto.randomUUID()
  await userRepository.setWidgetToken(session.userId, token)
  return NextResponse.json({ token })
}
