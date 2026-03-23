import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { redirect } from "next/navigation"
import { AppHeader } from "@/components/app-header"
import { eventSubSubscriptionsRepository, linkedAccountsRepository } from "@/repositories"
import { TwitchManage } from "./twitch-manage"
import { YouTubeConnectButton } from "./youtube-connect"
import { DisconnectButton } from "./disconnect-button"
import { ConnectionsUpdater } from "./connections-updater"

function TwitchLogo({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="currentColor" className={className} aria-hidden>
      <path d="M11.571 4.714h1.715v5.143H11.57zm4.715 0H18v5.143h-1.714zM6 0L1.714 4.286v15.428h5.143V24l4.286-4.286h3.428L22.286 12V0zm14.571 11.143l-3.428 3.428h-3.429l-3 3v-3H6.857V1.714h13.714z" />
    </svg>
  )
}

function SpotifyLogo({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="currentColor" className={className} aria-hidden>
      <path d="M12 0C5.4 0 0 5.4 0 12s5.4 12 12 12 12-5.4 12-12S18.66 0 12 0zm5.521 17.34c-.24.359-.66.48-1.021.24-2.82-1.74-6.36-2.101-10.561-1.141-.418.122-.779-.179-.899-.539-.12-.421.18-.78.54-.9 4.56-1.021 8.52-.6 11.64 1.32.42.18.479.659.301 1.02zm1.44-3.3c-.301.42-.841.6-1.262.3-3.239-1.98-8.159-2.58-11.939-1.38-.479.12-1.02-.12-1.14-.6-.12-.48.12-1.021.6-1.141C9.6 9.9 15 10.561 18.72 12.84c.361.181.54.78.241 1.2zm.12-3.36C15.24 8.4 8.82 8.16 5.16 9.301c-.6.179-1.2-.181-1.38-.721-.18-.601.18-1.2.72-1.381 4.26-1.26 11.28-1.02 15.721 1.621.539.3.719 1.02.419 1.56-.299.421-1.02.599-1.559.3z" />
    </svg>
  )
}

function YouTubeLogo({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="currentColor" className={className} aria-hidden>
      <path d="M23.498 6.186a3.016 3.016 0 0 0-2.122-2.136C19.505 3.545 12 3.545 12 3.545s-7.505 0-9.377.505A3.017 3.017 0 0 0 .502 6.186C0 8.07 0 12 0 12s0 3.93.502 5.814a3.016 3.016 0 0 0 2.122 2.136c1.871.505 9.376.505 9.376.505s7.505 0 9.377-.505a3.015 3.015 0 0 0 2.122-2.136C24 15.93 24 12 24 12s0-3.93-.502-5.814zM9.545 15.568V8.432L15.818 12l-6.273 3.568z" />
    </svg>
  )
}

type ConnectionRowProps = {
  name: string
  description: string
  connected: boolean
  logo: React.ReactNode
  detail?: string
  comingSoon?: boolean
  connectButton?: React.ReactNode
  disconnectButton?: React.ReactNode
  children?: React.ReactNode
}

function ConnectionRow({ name, description, connected, logo, detail, comingSoon, connectButton, disconnectButton, children }: ConnectionRowProps) {
  return (
    <div>
      <div className="px-4 sm:px-6 py-4 sm:py-5 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 sm:gap-6">
        <div className="flex items-start gap-4">
          <div className="shrink-0 mt-0.5">{logo}</div>
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
        </div>
        <div className="shrink-0 flex items-center gap-2 pl-9 sm:pl-0">
          {connected ? (
            <>
              {!detail && (
                <span className="flex items-center gap-1.5 text-xs text-green-500">
                  <span className="w-1.5 h-1.5 rounded-full bg-green-500 inline-block" />
                  Connected
                </span>
              )}
              {disconnectButton}
            </>
          ) : connectButton ?? (
            <button
              disabled={comingSoon}
              className="text-xs bg-purple-500 hover:bg-purple-600 text-white px-3 py-1.5 rounded-lg transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
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

const LINK_ERRORS: Record<string, string> = {
  account_conflict: "That account is already linked to a different CreatorDeck user.",
  no_youtube_channel: "No YouTube channel found on that Google account.",
  token_exchange_failed: "Google sign-in failed. Please try again.",
  missing_params: "Something went wrong with the connection flow. Please try again.",
  invalid_state: "Session expired during connection. Please try again.",
}

export default async function ConnectionsPage({
  searchParams,
}: {
  searchParams: { error?: string; linked?: string }
}) {
  const session = await getServerSession(authOptions)
  if (!session) redirect("/")

  const [subscriptionsRegistered, linkedAccounts] = await Promise.all([
    session.twitchId ? eventSubSubscriptionsRepository.existsByBroadcasterId(session.twitchId) : false,
    session.userId ? linkedAccountsRepository.findByUserId(session.userId) : [],
  ])

  const youtubeAccount = linkedAccounts.find(a => a.provider === "youtube")
  const twitchAccount = linkedAccounts.find(a => a.provider === "twitch")
  const canDisconnect = linkedAccounts.length > 1
  const webhookUrl = `${process.env.NEXT_PUBLIC_APP_URL}/api/webhook`

  const errorMessage = searchParams.error
    ? (LINK_ERRORS[searchParams.error] ?? "Something went wrong. Please try again.")
    : session.linkingError === "account_conflict"
    ? LINK_ERRORS.account_conflict
    : session.linkingError === "no_youtube_channel"
    ? LINK_ERRORS.no_youtube_channel
    : null

  return (
    <div className="min-h-screen">
      <ConnectionsUpdater />
      <AppHeader displayName={session.displayName} />
      <main className="max-w-3xl mx-auto px-6 py-10 space-y-6">
        <div>
          <h1 className="text-2xl font-bold">Connections</h1>
          <p className="text-zinc-500 text-sm mt-1">Manage the platforms and services connected to CreatorDeck.</p>
        </div>

        {errorMessage && (
          <div className="bg-red-50 dark:bg-red-950/20 border border-red-200 dark:border-red-800/40 rounded-xl px-4 py-3">
            <p className="text-sm font-medium text-red-700 dark:text-red-400">Connection failed</p>
            <p className="text-xs text-red-600 dark:text-red-500 mt-0.5">{errorMessage}</p>
          </div>
        )}

        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl divide-y divide-zinc-200 dark:divide-zinc-800">
          <ConnectionRow
            name="Twitch"
            description="Enables live event tracking, sub goals, and EventSub webhooks."
            connected={!!twitchAccount}
            logo={<TwitchLogo className="w-5 h-5 text-[#9146FF]" />}
            detail={twitchAccount ? `Connected as ${twitchAccount.displayName ?? twitchAccount.login}` : undefined}
            disconnectButton={canDisconnect ? <DisconnectButton provider="twitch" /> : undefined}
          >
            {twitchAccount && (
              <TwitchManage webhookUrl={webhookUrl} subscriptionsRegistered={subscriptionsRegistered} />
            )}
          </ConnectionRow>
          <ConnectionRow
            name="YouTube"
            description="Track Super Chats, memberships, and live chat activity."
            connected={!!youtubeAccount}
            logo={<YouTubeLogo className="w-5 h-5 text-[#FF0000]" />}
            detail={youtubeAccount ? `Connected as ${youtubeAccount.displayName ?? youtubeAccount.login ?? youtubeAccount.providerAccountId}` : undefined}
            connectButton={<YouTubeConnectButton />}
            disconnectButton={canDisconnect ? <DisconnectButton provider="youtube" /> : undefined}
          />
          <ConnectionRow
            name="Spotify"
            description="Show your currently playing track in the dashboard."
            connected={false}
            logo={<SpotifyLogo className="w-5 h-5 text-[#1DB954]" />}
            comingSoon={true}
          />
        </div>

        {!canDisconnect && (
          <p className="text-xs text-zinc-500 text-center">
            Connect another platform before disconnecting your only linked account.
          </p>
        )}
      </main>
    </div>
  )
}
