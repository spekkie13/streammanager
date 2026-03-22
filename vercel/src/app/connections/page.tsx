import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { redirect } from "next/navigation"
import { AppHeader } from "@/components/app-header"
import { eventSubSubscriptionsRepository } from "@/repositories"
import { TwitchManage } from "./twitch-manage"

type ConnectionRowProps = {
  name: string
  description: string
  connected: boolean
  detail?: string
  comingSoon?: boolean
  children?: React.ReactNode
}

function ConnectionRow({ name, description, connected, detail, comingSoon, children }: ConnectionRowProps) {
  return (
    <div>
      <div className="px-6 py-5 flex items-center justify-between gap-6">
        <div className="space-y-0.5">
          <div className="flex items-center gap-2">
            <span className="text-sm font-medium text-zinc-900 dark:text-white">{name}</span>
            {comingSoon && (
              <span className="text-xs text-zinc-500 bg-zinc-100 dark:bg-zinc-800 px-2 py-0.5 rounded">Coming soon</span>
            )}
          </div>
          <p className="text-xs text-zinc-500">{description}</p>
          {detail && <p className="text-xs text-zinc-600 dark:text-zinc-400 mt-1">{detail}</p>}
        </div>
        <div className="shrink-0 flex items-center gap-2">
          {connected ? (
            <>
              <span className="flex items-center gap-1.5 text-xs text-green-500">
                <span className="w-1.5 h-1.5 rounded-full bg-green-500 inline-block" />
                Connected
              </span>
              <button
                disabled
                className="text-xs text-zinc-500 border border-zinc-200 dark:border-zinc-800 px-3 py-1.5 rounded-lg opacity-50 cursor-not-allowed"
              >
                Disconnect
              </button>
            </>
          ) : (
            <button
              disabled={comingSoon}
              className="text-xs text-zinc-600 dark:text-zinc-400 border border-zinc-300 dark:border-zinc-700 px-3 py-1.5 rounded-lg hover:border-zinc-400 dark:hover:border-zinc-500 hover:text-zinc-900 dark:hover:text-white transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
            >
              Connect
            </button>
          )}
        </div>
      </div>
      {children}
    </div>
  )
}

export default async function ConnectionsPage() {
  const session = await getServerSession(authOptions)
  if (!session) redirect("/")

  const subscriptionsRegistered = await eventSubSubscriptionsRepository.existsByBroadcasterId(session.twitchId)
  const webhookUrl = `${process.env.NEXT_PUBLIC_APP_URL}/api/webhook`

  return (
    <div className="min-h-screen">
      <AppHeader displayName={session.displayName} />
      <main className="max-w-3xl mx-auto px-6 py-10 space-y-6">
        <div>
          <h1 className="text-2xl font-bold">Connections</h1>
          <p className="text-zinc-500 text-sm mt-1">Manage the platforms and services connected to CreatorDeck.</p>
        </div>

        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl divide-y divide-zinc-200 dark:divide-zinc-800">
          <ConnectionRow
            name="Twitch"
            description="Enables live event tracking, sub goals, and EventSub webhooks."
            connected={true}
            detail={`Connected as ${session.displayName}`}
          >
            <TwitchManage webhookUrl={webhookUrl} subscriptionsRegistered={subscriptionsRegistered} />
          </ConnectionRow>
          <ConnectionRow
            name="Spotify"
            description="Show your currently playing track in the dashboard."
            connected={false}
            comingSoon={true}
          />
          <ConnectionRow
            name="YouTube"
            description="Track Super Chats, memberships, and live chat activity."
            connected={false}
            comingSoon={true}
          />
        </div>
      </main>
    </div>
  )
}
