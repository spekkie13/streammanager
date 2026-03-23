"use client"
import { signIn } from "next-auth/react"

export function YouTubeConnectButton() {
  return (
    <button
      onClick={() => signIn("google", { callbackUrl: "/connections" })}
      className="text-xs bg-purple-500 hover:bg-purple-600 text-white px-3 py-1.5 rounded-lg transition-colors"
    >
      Connect
    </button>
  )
}
