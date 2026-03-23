"use client"

export function YouTubeConnectButton() {
  return (
    <a
      href="/api/connections/link/google/start"
      className="text-xs bg-purple-500 hover:bg-purple-600 text-white px-3 py-1.5 rounded-lg transition-colors"
    >
      Connect
    </a>
  )
}
