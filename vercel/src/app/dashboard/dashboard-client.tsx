"use client"
import { useState } from "react"
import Link from "next/link"
import type { Session } from "next-auth"
import { AppHeader } from "@/components/app-header"
import { useStreamEvents } from "@/hooks/use-stream-events"
import type { LiveEvent, LiveEventType } from "@/types/events"

type Props = {
  session: Session
  goal: number
  total: number
  initialEvents: LiveEvent[]
  subscriptionsRegistered: boolean
}

const TYPE_BADGE: Record<LiveEventType, string> = {
  sub:    "bg-purple-500/20 text-purple-400 border border-purple-500/40",
  follow: "bg-blue-500/20 text-blue-400 border border-blue-500/40",
  bits:   "bg-yellow-500/20 text-yellow-500 border border-yellow-500/40",
  raid:   "bg-green-500/20 text-green-500 border border-green-500/40",
}

const TYPE_ICON: Record<LiveEventType, string> = {
  sub:    "★",
  follow: "♥",
  bits:   "◆",
  raid:   "▶",
}

function formatAmount(type: LiveEventType, amount: number | null): string | null {
  if (amount === null) return null
  if (type === "bits") return `${amount.toLocaleString()} bits`
  if (type === "raid") return `${amount.toLocaleString()} viewers`
  return null
}

export function DashboardClient({ session, goal, total, initialEvents, subscriptionsRegistered }: Props) {
  const [currentGoal, setCurrentGoal] = useState(goal)
  const [goalInput, setGoalInput] = useState(String(goal))
  const [savingGoal, setSavingGoal] = useState(false)

  const events = useStreamEvents(initialEvents)
  const progress = Math.min((total / currentGoal) * 100, 100)

  async function saveGoal() {
    const val = parseInt(goalInput)
    if (isNaN(val) || val < 1) return
    setSavingGoal(true)
    await fetch("/api/goal", { method: "POST", body: JSON.stringify({ goal: val }), headers: { "Content-Type": "application/json" } })
    setCurrentGoal(val)
    setSavingGoal(false)
  }

  return (
    <div className="min-h-screen">
      <AppHeader displayName={session.displayName} />

      <main className="max-w-5xl mx-auto px-6 py-10 space-y-6">

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

        {/* Sub goal */}
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl p-6 space-y-4">
          <h2 className="text-sm font-medium text-zinc-500 dark:text-zinc-400 uppercase tracking-wider">Sub Goal</h2>
          <div className="flex items-end gap-3">
            <span className="text-5xl font-bold">{total}</span>
            <span className="text-2xl text-zinc-400 dark:text-zinc-500 pb-1">/ {currentGoal}</span>
          </div>
          <div className="w-full bg-zinc-200 dark:bg-zinc-800 rounded-full h-3">
            <div
              className="bg-purple-500 h-3 rounded-full transition-all duration-500"
              style={{ width: `${progress}%` }}
            />
          </div>
          <p className="text-zinc-500 text-sm">{progress.toFixed(1)}% of goal reached</p>

          <div className="flex items-center gap-3 pt-2">
            <input
              type="number"
              value={goalInput}
              onChange={e => setGoalInput(e.target.value)}
              className="bg-zinc-100 dark:bg-zinc-800 border border-zinc-300 dark:border-zinc-700 rounded-lg px-3 py-2 text-sm w-28 focus:outline-none focus:border-purple-500 text-zinc-900 dark:text-white"
              min={1}
            />
            <button
              onClick={saveGoal}
              disabled={savingGoal}
              className="bg-purple-500 hover:bg-purple-600 disabled:opacity-50 text-white text-sm font-medium px-4 py-2 rounded-lg transition-colors"
            >
              {savingGoal ? "Saving..." : "Set Goal"}
            </button>
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
                  <span className={`shrink-0 text-xs px-2 py-0.5 rounded font-medium ${TYPE_BADGE[event.type]}`}>
                    {TYPE_ICON[event.type]} {event.type}
                  </span>
                  <span className="flex-1 text-sm truncate">{event.fromUser}</span>
                  {event.amount !== null && (
                    <span className="text-sm text-zinc-500 dark:text-zinc-400 shrink-0">
                      {formatAmount(event.type, event.amount)}
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
