"use client"
import { useStreamEvents } from "@/hooks/use-stream-events"
import Link from "next/link"
import type { Session } from "next-auth"
import { AppHeader } from "@/components/app-header"
import type { LiveEvent, LiveEventType } from "@/types/events"

type Props = {
  session: Session
  goal: number
  initialCount: number
  endsAt: string | null
  total: number
  initialEvents: LiveEvent[]
  subscriptionsRegistered: boolean
  followerCount: number | null
  subCount: number | null
  ytSubCount: number | null
  hasYouTube: boolean
  followerGrowth: number
  subGrowth: number
  followTotal: number
  followGoal: number | null
  ytMemberTotal: number
  ytMemberGoal: number | null
}

const TYPE_BADGE: Record<LiveEventType, string> = {
  sub:       "bg-purple-500/20 text-purple-400 border border-purple-500/40",
  follow:    "bg-blue-500/20 text-blue-400 border border-blue-500/40",
  bits:      "bg-yellow-500/20 text-yellow-500 border border-yellow-500/40",
  raid:      "bg-green-500/20 text-green-500 border border-green-500/40",
  superchat: "bg-red-500/20 text-red-400 border border-red-500/40",
  member:    "bg-orange-500/20 text-orange-400 border border-orange-500/40",
}

const TYPE_ICON: Record<LiveEventType, string> = {
  sub:       "★",
  follow:    "♥",
  bits:      "◆",
  raid:      "▶",
  superchat: "💬",
  member:    "🎖",
}

function formatAmount(type: LiveEventType, amount: number | null, currency?: string | null): string | null {
  if (amount === null) return null
  if (type === "bits") return `${amount.toLocaleString()} bits`
  if (type === "raid") return `${amount.toLocaleString()} viewers`
  if (type === "member") return `${amount} mo.`
  if (type === "superchat") {
    return currency
      ? new Intl.NumberFormat(undefined, { style: "currency", currency }).format(amount)
      : `${amount}`
  }
  return null
}

function greeting(): string {
  const h = new Date().getHours()
  if (h < 12) return "Good morning"
  if (h < 18) return "Good afternoon"
  return "Good evening"
}

function formatCount(n: number | null): string {
  if (n === null) return "—"
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`
  if (n >= 1_000) return `${(n / 1_000).toFixed(1)}K`
  return n.toLocaleString()
}

function TwitchLogo({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="currentColor" className={className} aria-hidden>
      <path d="M11.571 4.714h1.715v5.143H11.57zm4.715 0H18v5.143h-1.714zM6 0L1.714 4.286v15.428h5.143V24l4.286-4.286h3.428L22.286 12V0zm14.571 11.143l-3.428 3.428h-3.429l-3 3v-3H6.857V1.714h13.714z" />
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

type StatusVariant = "good" | "warning"

const STATUS_CONFIG: Record<StatusVariant, { label: string; subtext: string; pill: string; dot: string }> = {
  good: {
    label: "All good",
    subtext: "Everything is set up and ready to go. Have a great stream!",
    pill: "bg-green-500/10 border-green-500/20 text-green-600 dark:text-green-400",
    dot: "bg-green-500",
  },
  warning: {
    label: "Action required",
    subtext: "There are a few things to set up before you're ready to go.",
    pill: "bg-amber-500/10 border-amber-500/20 text-amber-600 dark:text-amber-400",
    dot: "bg-amber-500",
  },
}

export function DashboardClient({
  session, goal, initialCount, endsAt, total, initialEvents,
  subscriptionsRegistered, followerCount, subCount, ytSubCount,
  hasYouTube, followerGrowth, subGrowth, followTotal, followGoal,
  ytMemberTotal, ytMemberGoal,
}: Props) {
  const events = useStreamEvents(initialEvents)

  const displayTotal = total + initialCount
  const progress = Math.min((displayTotal / goal) * 100, 100)

  const variant: StatusVariant = subscriptionsRegistered ? "good" : "warning"
  const s = STATUS_CONFIG[variant]

  return (
    <div className="min-h-screen">
      <AppHeader displayName={session.displayName} />

      <main className="max-w-5xl mx-auto px-6 py-10 space-y-6">

        {/* Welcome card */}
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl px-6 py-5 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div className="space-y-1">
            <h1 className="text-xl font-semibold tracking-tight">
              {greeting()}, <span className="text-purple-500">{session.displayName}</span> 👋
            </h1>
            <p className="text-sm text-zinc-500 dark:text-zinc-400">{s.subtext}</p>
          </div>
          <span className={`inline-flex items-center gap-2 text-xs font-medium px-3 py-1.5 rounded-full border shrink-0 ${s.pill}`}>
            <span className={`w-1.5 h-1.5 rounded-full ${s.dot}`} />
            {s.label}
          </span>
        </div>

        {/* Quick actions */}
        <div className="grid grid-cols-2 gap-3">
          <Link
            href="/goals"
            className="group bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 hover:border-purple-500/50 dark:hover:border-purple-500/40 rounded-xl px-5 py-4 flex items-center justify-between transition-colors"
          >
            <div>
              <p className="text-sm font-semibold">Goals</p>
              <p className="text-xs text-zinc-500 dark:text-zinc-400 mt-0.5">Manage your subscriber goal</p>
            </div>
            <span className="text-zinc-400 group-hover:text-purple-500 transition-colors text-lg">→</span>
          </Link>
          <Link
            href="/connections"
            className="group bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 hover:border-zinc-400 dark:hover:border-zinc-600 rounded-xl px-5 py-4 flex items-center justify-between transition-colors"
          >
            <div>
              <p className="text-sm font-semibold">Connections</p>
              <p className="text-xs text-zinc-500 dark:text-zinc-400 mt-0.5">Manage linked accounts</p>
            </div>
            <span className="text-zinc-400 group-hover:text-zinc-700 dark:group-hover:text-zinc-200 transition-colors text-lg">→</span>
          </Link>
        </div>

        {/* Setup banner */}
        {!subscriptionsRegistered && (
          <div className="bg-amber-50 dark:bg-amber-950/20 border border-amber-200 dark:border-amber-800/40 rounded-xl p-4 flex items-start gap-3">
            <span className="text-amber-500 text-base mt-0.5">⚠</span>
            <div>
              <p className="text-sm font-medium text-amber-800 dark:text-amber-300">Setup required</p>
              <p className="text-xs text-amber-700 dark:text-amber-500 mt-0.5">
                Register your Twitch EventSub subscriptions to start receiving live events.{" "}
                <Link href="/connections" className="underline hover:no-underline">Go to Connections →</Link>
              </p>
            </div>
          </div>
        )}

        {/* Audience pills */}
        <div className="grid grid-cols-3 gap-3">
          <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl px-5 py-4 space-y-1">
            <div className="flex items-center gap-1.5 text-xs text-zinc-500 dark:text-zinc-400">
              <TwitchLogo className="w-3.5 h-3.5 text-[#9146FF]" />
              Followers
            </div>
            <p className="text-2xl font-bold">{formatCount(followerCount)}</p>
            <p className={`text-xs ${followerGrowth > 0 ? "text-green-500" : "text-zinc-400 dark:text-zinc-600"}`}>
              {followerGrowth > 0 ? `+${followerGrowth.toLocaleString()}` : "—"} last 30d
            </p>
          </div>

          <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl px-5 py-4 space-y-1">
            <div className="flex items-center gap-1.5 text-xs text-zinc-500 dark:text-zinc-400">
              <TwitchLogo className="w-3.5 h-3.5 text-[#9146FF]" />
              Subscribers
            </div>
            <p className="text-2xl font-bold">{formatCount(subCount)}</p>
            <p className={`text-xs ${subGrowth > 0 ? "text-green-500" : "text-zinc-400 dark:text-zinc-600"}`}>
              {subGrowth > 0 ? `+${subGrowth.toLocaleString()}` : "—"} last 30d
            </p>
          </div>

          <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl px-5 py-4 space-y-1">
            <div className="flex items-center gap-1.5 text-xs text-zinc-500 dark:text-zinc-400">
              <YouTubeLogo className="w-3.5 h-3.5 text-[#FF0000]" />
              Subscribers
            </div>
            <p className="text-2xl font-bold">{formatCount(hasYouTube ? ytSubCount : null)}</p>
            <p className="text-xs text-zinc-400 dark:text-zinc-600">— last 30d</p>
          </div>
        </div>

        {/* Goals */}
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl p-6 space-y-4">
          <div className="flex items-center justify-between">
            <h2 className="text-sm font-medium text-zinc-500 dark:text-zinc-400 uppercase tracking-wider">Goals</h2>
            <Link href="/goals" className="text-xs text-purple-500 hover:text-purple-400 transition-colors">
              Manage →
            </Link>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">

            {/* Twitch sub goal — always active */}
            <div className="bg-zinc-50 dark:bg-zinc-800/50 border border-zinc-200 dark:border-zinc-700/60 rounded-lg p-4 space-y-2">
              <div className="flex items-center gap-1.5 text-xs text-zinc-500 dark:text-zinc-400">
                <TwitchLogo className="w-3.5 h-3.5 text-[#9146FF]" />
                Twitch Subscribers
              </div>
              <div className="flex items-baseline gap-1.5">
                <span className="text-2xl font-bold">{displayTotal.toLocaleString()}</span>
                <span className="text-sm text-zinc-400 dark:text-zinc-500">/ {goal.toLocaleString()}</span>
              </div>
              <div className="w-full bg-zinc-200 dark:bg-zinc-700 rounded-full h-2">
                <div className="bg-purple-500 h-2 rounded-full transition-all duration-500" style={{ width: `${progress}%` }} />
              </div>
              <p className="text-xs text-zinc-500">{progress.toFixed(1)}%</p>
            </div>

            {/* Twitch follow goal */}
            {followGoal !== null ? (() => {
              const followProgress = Math.min((followTotal / followGoal) * 100, 100)
              return (
                <div className="bg-zinc-50 dark:bg-zinc-800/50 border border-zinc-200 dark:border-zinc-700/60 rounded-lg p-4 space-y-2">
                  <div className="flex items-center gap-1.5 text-xs text-zinc-500 dark:text-zinc-400">
                    <TwitchLogo className="w-3.5 h-3.5 text-[#9146FF]" />
                    Twitch Followers
                  </div>
                  <div className="flex items-baseline gap-1.5">
                    <span className="text-2xl font-bold">{followTotal.toLocaleString()}</span>
                    <span className="text-sm text-zinc-400 dark:text-zinc-500">/ {followGoal.toLocaleString()}</span>
                  </div>
                  <div className="w-full bg-zinc-200 dark:bg-zinc-700 rounded-full h-2">
                    <div className="bg-blue-500 h-2 rounded-full transition-all duration-500" style={{ width: `${followProgress}%` }} />
                  </div>
                  <p className="text-xs text-zinc-500">{followProgress.toFixed(1)}%</p>
                </div>
              )
            })() : (
              /* No goal set */
              <div className="bg-zinc-50 dark:bg-zinc-800/50 border border-dashed border-zinc-300 dark:border-zinc-700 rounded-lg p-4 space-y-2">
                <div className="flex items-center gap-1.5 text-xs text-zinc-400 dark:text-zinc-500">
                  <TwitchLogo className="w-3.5 h-3.5 text-zinc-400 dark:text-zinc-600" />
                  Twitch Followers
                </div>
                <p className="text-xs text-zinc-400 dark:text-zinc-500">No follow goal set.</p>
                <div className="w-full bg-zinc-200 dark:bg-zinc-700 rounded-full h-2" />
                <Link href="/goals" className="text-xs text-purple-500 hover:text-purple-400 transition-colors">Set a goal →</Link>
              </div>
            )}

            {/* YouTube member goal */}
            {!hasYouTube ? (
              /* Not connected — amber tint */
              <div className="bg-amber-50/60 dark:bg-amber-950/20 border border-dashed border-amber-300 dark:border-amber-700/50 rounded-lg p-4 space-y-2">
                <div className="flex items-center gap-1.5 text-xs text-zinc-400 dark:text-zinc-500">
                  <YouTubeLogo className="w-3.5 h-3.5 text-zinc-400 dark:text-zinc-600" />
                  YouTube Members
                </div>
                <p className="text-xs font-medium text-amber-600 dark:text-amber-400">YouTube not connected</p>
                <div className="w-full bg-amber-100 dark:bg-amber-900/20 rounded-full h-2" />
                <Link href="/connections" className="text-xs text-purple-500 hover:text-purple-400 transition-colors">Connect account →</Link>
              </div>
            ) : ytMemberGoal !== null ? (() => {
              const memberProgress = Math.min((ytMemberTotal / ytMemberGoal) * 100, 100)
              return (
                <div className="bg-zinc-50 dark:bg-zinc-800/50 border border-zinc-200 dark:border-zinc-700/60 rounded-lg p-4 space-y-2">
                  <div className="flex items-center gap-1.5 text-xs text-zinc-500 dark:text-zinc-400">
                    <YouTubeLogo className="w-3.5 h-3.5 text-[#FF0000]" />
                    YouTube Members
                  </div>
                  <div className="flex items-baseline gap-1.5">
                    <span className="text-2xl font-bold">{ytMemberTotal.toLocaleString()}</span>
                    <span className="text-sm text-zinc-400 dark:text-zinc-500">/ {ytMemberGoal.toLocaleString()}</span>
                  </div>
                  <div className="w-full bg-zinc-200 dark:bg-zinc-700 rounded-full h-2">
                    <div className="bg-red-500 h-2 rounded-full transition-all duration-500" style={{ width: `${memberProgress}%` }} />
                  </div>
                  <p className="text-xs text-zinc-500">{memberProgress.toFixed(1)}%</p>
                </div>
              )
            })() : (
              /* Connected but no goal set */
              <div className="bg-zinc-50 dark:bg-zinc-800/50 border border-dashed border-zinc-300 dark:border-zinc-700 rounded-lg p-4 space-y-2">
                <div className="flex items-center gap-1.5 text-xs text-zinc-400 dark:text-zinc-500">
                  <YouTubeLogo className="w-3.5 h-3.5 text-zinc-400 dark:text-zinc-600" />
                  YouTube Members
                </div>
                <p className="text-xs text-zinc-400 dark:text-zinc-500">No member goal set.</p>
                <div className="w-full bg-zinc-200 dark:bg-zinc-700 rounded-full h-2" />
                <Link href="/goals" className="text-xs text-purple-500 hover:text-purple-400 transition-colors">Set a goal →</Link>
              </div>
            )}

          </div>
        </div>

        {/* Live event feed */}
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl overflow-hidden">
          <div className="px-6 py-4 border-b border-zinc-200 dark:border-zinc-800 flex items-center justify-between">
            <div className="flex items-center gap-3">
              <h2 className="text-sm font-medium text-zinc-500 dark:text-zinc-400 uppercase tracking-wider">Live Feed</h2>
              <span className="flex items-center gap-1.5 text-xs text-green-500">
                <span className="w-1.5 h-1.5 rounded-full bg-green-500 animate-pulse inline-block" />
                Live
              </span>
            </div>
            <Link href="/events" className="text-xs text-purple-500 hover:text-purple-400 transition-colors">
              View all events →
            </Link>
          </div>

          {events.length === 0 ? (
            <div className="px-6 py-12 text-center text-zinc-500 text-sm">
              Waiting for events... Subs, follows, bits and raids will appear here in real time.
            </div>
          ) : (
            <div className="divide-y divide-zinc-200 dark:divide-zinc-800/60">
              {events.map(event => (
                <div key={event.id} className="px-6 py-3 flex items-center gap-4">
                  {event.platform === "youtube"
                    ? <YouTubeLogo className="shrink-0 w-3 h-3 text-[#FF0000]" />
                    : <TwitchLogo className="shrink-0 w-3 h-3 text-[#9146FF]" />
                  }
                  <span className={`shrink-0 text-xs px-2 py-0.5 rounded font-medium ${TYPE_BADGE[event.type]}`}>
                    {TYPE_ICON[event.type]} {event.type}
                  </span>
                  <span className="flex-1 text-sm truncate">{event.fromUser}</span>
                  {event.amount !== null && (
                    <span className="text-sm text-zinc-500 dark:text-zinc-400 shrink-0">
                      {formatAmount(event.type, event.amount, event.currency)}
                    </span>
                  )}
                  <span className="text-xs text-zinc-400 dark:text-zinc-600 shrink-0">
                    {new Date(event.occurredAt).toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit" })}
                  </span>
                </div>
              ))}
            </div>
          )}
        </div>

      </main>
    </div>
  )
}
