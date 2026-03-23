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
  initialCount: number
  endsAt: string | null
  total: number
  initialEvents: LiveEvent[]
  subscriptionsRegistered: boolean
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

function toDateInputValue(iso: string | null): string {
  if (!iso) return ""
  return iso.slice(0, 10) // "YYYY-MM-DD"
}

function greeting(): string {
  const h = new Date().getHours()
  if (h < 12) return "Good morning"
  if (h < 18) return "Good afternoon"
  return "Good evening"
}

type StatusVariant = "good" | "warning" | "unknown"

const STATUS_CONFIG: Record<StatusVariant, {
  label: string
  subtext: string
  pill: string
  dot: string
}> = {
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
  unknown: {
    label: "Status unknown",
    subtext: "Some services couldn't be reached. Check your connections.",
    pill: "bg-zinc-100 dark:bg-zinc-800 border-zinc-200 dark:border-zinc-700 text-zinc-500",
    dot: "bg-zinc-400",
  },
}

export function DashboardClient({ session, goal, initialCount, endsAt, total, initialEvents, subscriptionsRegistered }: Props) {
  const [currentGoal, setCurrentGoal] = useState(goal)
  const [goalInput, setGoalInput] = useState(String(goal))
  const [initialCountInput, setInitialCountInput] = useState(String(initialCount))
  const [endsAtInput, setEndsAtInput] = useState(toDateInputValue(endsAt))
  const [savingGoal, setSavingGoal] = useState(false)
  const [editing, setEditing] = useState(false)

  const events = useStreamEvents(initialEvents)
  const savedInitialCount = parseInt(initialCountInput) || 0
  const displayTotal = total + savedInitialCount
  const progress = Math.min((displayTotal / currentGoal) * 100, 100)

  async function saveGoal() {
    const val = parseInt(goalInput)
    if (isNaN(val) || val < 1) return
    setSavingGoal(true)
    const initialCountVal = Math.max(0, parseInt(initialCountInput) || 0)
    await fetch("/api/goal", {
      method: "POST",
      body: JSON.stringify({ goal: val, initialCount: initialCountVal, endsAt: endsAtInput || null }),
      headers: { "Content-Type": "application/json" },
    })
    setCurrentGoal(val)
    setSavingGoal(false)
    setEditing(false)
  }

  function cancelEdit() {
    setGoalInput(String(currentGoal))
    setInitialCountInput(String(savedInitialCount))
    setEndsAtInput(toDateInputValue(endsAt))
    setEditing(false)
  }

  return (
    <div className="min-h-screen">
      <AppHeader displayName={session.displayName} />

      <main className="max-w-5xl mx-auto px-6 py-10 space-y-6">

        {/* Welcome card */}
        {(() => {
          const variant: StatusVariant = subscriptionsRegistered ? "good" : "warning"
          const s = STATUS_CONFIG[variant]
          return (
            <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl px-6 py-5 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
              <div className="space-y-1">
                <h1 className="text-xl font-semibold tracking-tight">
                  {greeting()}, <span className="text-purple-500">{session.displayName}</span> 👋
                </h1>
                <p className="text-sm text-zinc-500 dark:text-zinc-400">{s.subtext}</p>
              </div>
              <span className={`shrink-0 self-start sm:self-auto inline-flex items-center gap-2 text-xs font-medium px-3 py-1.5 rounded-full border ${s.pill}`}>
                <span className={`w-1.5 h-1.5 rounded-full ${s.dot}`} />
                {s.label}
              </span>
            </div>
          )
        })()}

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
          <div className="flex items-center justify-between">
            <h2 className="text-sm font-medium text-zinc-500 dark:text-zinc-400 uppercase tracking-wider">Twitch — Sub Goal</h2>
            <div className="flex items-center gap-3">
              {!editing && endsAtInput && (
                <span className="text-xs text-zinc-500 dark:text-zinc-400">
                  Ends {new Date(endsAtInput).toLocaleDateString(undefined, { month: "short", day: "numeric", year: "numeric" })}
                </span>
              )}
              {!editing && (
                <button
                  onClick={() => setEditing(true)}
                  className="text-xs text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-300 transition-colors"
                >
                  Edit
                </button>
              )}
            </div>
          </div>

          <div className="flex items-end gap-3">
            <span className="text-5xl font-bold">{displayTotal}</span>
            <span className="text-2xl text-zinc-400 dark:text-zinc-500 pb-1">/ {currentGoal}</span>
          </div>
          <div className="w-full bg-zinc-200 dark:bg-zinc-800 rounded-full h-3">
            <div
              className="bg-purple-500 h-3 rounded-full transition-all duration-500"
              style={{ width: `${progress}%` }}
            />
          </div>
          <p className="text-zinc-500 text-sm">{progress.toFixed(1)}% of goal reached</p>

          {editing && (
            <div className="pt-4 border-t border-zinc-200 dark:border-zinc-800 space-y-4">
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                <div className="flex flex-col gap-1.5">
                  <label className="text-xs font-medium text-zinc-500 dark:text-zinc-400">Initial amount</label>
                  <input
                    type="number"
                    value={initialCountInput}
                    onChange={e => setInitialCountInput(e.target.value)}
                    className="bg-zinc-100 dark:bg-zinc-800 border border-zinc-300 dark:border-zinc-700 rounded-lg px-3 py-2 text-sm focus:outline-none focus:border-purple-500 text-zinc-900 dark:text-white"
                    min={0}
                  />
                </div>
                <div className="flex flex-col gap-1.5">
                  <label className="text-xs font-medium text-zinc-500 dark:text-zinc-400">Goal amount</label>
                  <input
                    type="number"
                    value={goalInput}
                    onChange={e => setGoalInput(e.target.value)}
                    className="bg-zinc-100 dark:bg-zinc-800 border border-zinc-300 dark:border-zinc-700 rounded-lg px-3 py-2 text-sm focus:outline-none focus:border-purple-500 text-zinc-900 dark:text-white"
                    min={1}
                  />
                </div>
                <div className="flex flex-col gap-1.5">
                  <label className="text-xs font-medium text-zinc-500 dark:text-zinc-400">
                    End date
                    <span className="ml-1 font-normal text-zinc-400 dark:text-zinc-600">(optional)</span>
                  </label>
                  <div className="flex items-center gap-2">
                    <input
                      type="date"
                      value={endsAtInput}
                      onChange={e => setEndsAtInput(e.target.value)}
                      className="flex-1 bg-zinc-100 dark:bg-zinc-800 border border-zinc-300 dark:border-zinc-700 rounded-lg px-3 py-2 text-sm focus:outline-none focus:border-purple-500 text-zinc-900 dark:text-white"
                    />
                    {endsAtInput && (
                      <button
                        onClick={() => setEndsAtInput("")}
                        className="text-xs text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-300 transition-colors shrink-0"
                      >
                        Clear
                      </button>
                    )}
                  </div>
                </div>
              </div>
              <div className="flex items-center gap-3">
                <button
                  onClick={saveGoal}
                  disabled={savingGoal}
                  className="bg-purple-500 hover:bg-purple-600 disabled:opacity-50 text-white text-sm font-medium px-5 py-2 rounded-lg transition-colors"
                >
                  {savingGoal ? "Saving..." : "Save"}
                </button>
                <button
                  onClick={cancelEdit}
                  className="text-sm text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-300 transition-colors"
                >
                  Cancel
                </button>
              </div>
            </div>
          )}
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
