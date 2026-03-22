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
  webhookUrl: string
  initialEvents: LiveEvent[]
}

const TYPE_BADGE: Record<LiveEventType, string> = {
  sub:    "bg-purple-500/20 text-purple-300 border border-purple-500/40",
  follow: "bg-blue-500/20 text-blue-300 border border-blue-500/40",
  bits:   "bg-yellow-500/20 text-yellow-300 border border-yellow-500/40",
  raid:   "bg-green-500/20 text-green-300 border border-green-500/40",
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

export function DashboardClient({ session, goal, total, webhookUrl, initialEvents }: Props) {
  const [currentGoal, setCurrentGoal] = useState(goal)
  const [goalInput, setGoalInput] = useState(String(goal))
  const [savingGoal, setSavingGoal] = useState(false)
  const [registering, setRegistering] = useState(false)
  const [registerStatus, setRegisterStatus] = useState<string | null>(null)
  const [copied, setCopied] = useState<string | null>(null)

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

  async function registerSubscriptions() {
    setRegistering(true)
    setRegisterStatus(null)
    const res = await fetch("/api/register-subscriptions", { method: "POST" })
    if (res.ok) setRegisterStatus("Subscriptions registered successfully!")
    else setRegisterStatus("Failed to register — check your Twitch app scopes.")
    setRegistering(false)
  }

  function copy(text: string, key: string) {
    navigator.clipboard.writeText(text)
    setCopied(key)
    setTimeout(() => setCopied(null), 2000)
  }

  return (
    <div className="min-h-screen bg-[#0a0a0a] text-white">
      <AppHeader displayName={session.displayName} />

      <main className="max-w-5xl mx-auto px-6 py-10 space-y-8">

        {/* Sub goal */}
        <div className="bg-zinc-900 border border-zinc-800 rounded-xl p-6 space-y-4">
          <h2 className="text-sm font-medium text-zinc-400 uppercase tracking-wider">Sub Goal</h2>
          <div className="flex items-end gap-3">
            <span className="text-5xl font-bold">{total}</span>
            <span className="text-2xl text-zinc-500 pb-1">/ {currentGoal}</span>
          </div>
          <div className="w-full bg-zinc-800 rounded-full h-3">
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
              className="bg-zinc-800 border border-zinc-700 rounded-lg px-3 py-2 text-sm w-28 focus:outline-none focus:border-purple-500"
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

        {/* Two-column: bot config + register */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">

          {/* Bot config */}
          <div className="bg-zinc-900 border border-zinc-800 rounded-xl p-6 space-y-4">
            <h2 className="text-sm font-medium text-zinc-400 uppercase tracking-wider">Bot Integration</h2>
            <div className="space-y-3">
              <div>
                <label className="text-xs text-zinc-500 mb-1 block">Webhook URL</label>
                <div className="flex items-center gap-2">
                  <code className="flex-1 bg-zinc-800 text-xs text-zinc-300 px-3 py-2 rounded-lg truncate">{webhookUrl}</code>
                  <button onClick={() => copy(webhookUrl, "webhook")} className="text-xs text-zinc-400 hover:text-white px-2 py-2 rounded transition-colors">
                    {copied === "webhook" ? "✓" : "Copy"}
                  </button>
                </div>
              </div>
              <div>
                <label className="text-xs text-zinc-500 mb-1 block">API Key</label>
                <div className="flex items-center gap-2">
                  <code className="flex-1 bg-zinc-800 text-xs text-zinc-300 px-3 py-2 rounded-lg truncate">{session.apiKey}</code>
                  <button onClick={() => copy(session.apiKey, "apiKey")} className="text-xs text-zinc-400 hover:text-white px-2 py-2 rounded transition-colors">
                    {copied === "apiKey" ? "✓" : "Copy"}
                  </button>
                </div>
              </div>
            </div>
          </div>

          {/* Register subscriptions */}
          <div className="bg-zinc-900 border border-zinc-800 rounded-xl p-6 space-y-4">
            <h2 className="text-sm font-medium text-zinc-400 uppercase tracking-wider">Twitch EventSub</h2>
            <p className="text-zinc-400 text-sm">Register webhook subscriptions so Twitch delivers events to this service.</p>
            <button
              onClick={registerSubscriptions}
              disabled={registering}
              className="bg-zinc-800 hover:bg-zinc-700 disabled:opacity-50 border border-zinc-700 text-white text-sm font-medium px-4 py-2 rounded-lg transition-colors"
            >
              {registering ? "Registering..." : "Register Subscriptions"}
            </button>
            {registerStatus && (
              <p className={`text-sm ${registerStatus.includes("success") ? "text-green-400" : "text-red-400"}`}>
                {registerStatus}
              </p>
            )}
          </div>
        </div>

        {/* Live event feed */}
        <div className="bg-zinc-900 border border-zinc-800 rounded-xl overflow-hidden">
          <div className="px-6 py-4 border-b border-zinc-800 flex items-center justify-between">
            <div className="flex items-center gap-3">
              <h2 className="text-sm font-medium text-zinc-400 uppercase tracking-wider">Live Feed</h2>
              <span className="flex items-center gap-1.5 text-xs text-green-400">
                <span className="w-1.5 h-1.5 rounded-full bg-green-400 animate-pulse inline-block" />
                Live
              </span>
            </div>
            <Link href="/events" className="text-xs text-purple-400 hover:text-purple-300 transition-colors">
              View all events →
            </Link>
          </div>

          {events.length === 0 ? (
            <div className="px-6 py-12 text-center text-zinc-500 text-sm">
              Waiting for events... Subs, follows, bits and raids will appear here in real time.
            </div>
          ) : (
            <div className="divide-y divide-zinc-800/60">
              {events.map(event => (
                <div key={event.id} className="px-6 py-3 flex items-center gap-4">
                  <span className={`shrink-0 text-xs px-2 py-0.5 rounded font-medium ${TYPE_BADGE[event.type]}`}>
                    {TYPE_ICON[event.type]} {event.type}
                  </span>
                  <span className="flex-1 text-sm text-white truncate">{event.fromUser}</span>
                  {event.amount !== null && (
                    <span className="text-sm text-zinc-400 shrink-0">
                      {formatAmount(event.type, event.amount)}
                    </span>
                  )}
                  <span className="text-xs text-zinc-600 shrink-0">
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
