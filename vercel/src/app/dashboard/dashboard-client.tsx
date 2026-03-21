"use client"
import { useState } from "react"
import { signOut } from "next-auth/react"
import type { Session } from "next-auth"

type SubEvent = {
  id: string
  userId: string | null
  userDisplayName: string | null
  gifterId: string | null
  gifterDisplayName: string | null
  tier: string
  kind: string
  giftCount: number | null
  cumulativeMonths: number | null
  occurredAt: string
}

type Props = {
  session: Session
  goal: number
  total: number
  recentSubs: SubEvent[]
  webhookUrl: string
}

function tierLabel(tier: string) {
  return tier === "prime" ? "Prime" : tier === "1000" ? "T1" : tier === "2000" ? "T2" : tier === "3000" ? "T3" : tier
}

function kindLabel(kind: string, event: SubEvent) {
  if (kind === "new") return `${event.userDisplayName ?? "Someone"} subscribed (${tierLabel(event.tier)})`
  if (kind === "resub") return `${event.userDisplayName ?? "Someone"} resubscribed (${event.cumulativeMonths ?? "?"} months, ${tierLabel(event.tier)})`
  if (kind === "community_gift") return `${event.gifterDisplayName ?? "Anonymous"} gifted ${event.giftCount ?? 1} subs`
  return kind
}

export function DashboardClient({ session, goal, total, recentSubs, webhookUrl }: Props) {
  const [currentGoal, setCurrentGoal] = useState(goal)
  const [goalInput, setGoalInput] = useState(String(goal))
  const [savingGoal, setSavingGoal] = useState(false)
  const [registering, setRegistering] = useState(false)
  const [registerStatus, setRegisterStatus] = useState<string | null>(null)
  const [copied, setCopied] = useState<string | null>(null)

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
      {/* Header */}
      <header className="border-b border-zinc-800 px-6 py-4 flex items-center justify-between">
        <span className="text-xl font-bold">Stream<span className="text-purple-500">Stats</span></span>
        <div className="flex items-center gap-4">
          <span className="text-zinc-400 text-sm">{session.displayName}</span>
          <button onClick={() => signOut({ callbackUrl: "/" })} className="text-sm text-zinc-500 hover:text-zinc-300 transition-colors">
            Sign out
          </button>
        </div>
      </header>

      <main className="max-w-5xl mx-auto px-6 py-10 space-y-8">

        {/* Goal progress */}
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
            <p className="text-zinc-400 text-sm">Register webhook subscriptions so Twitch delivers sub events to this service.</p>
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

        {/* Recent subs */}
        <div className="bg-zinc-900 border border-zinc-800 rounded-xl overflow-hidden">
          <div className="px-6 py-4 border-b border-zinc-800">
            <h2 className="text-sm font-medium text-zinc-400 uppercase tracking-wider">Recent Subs</h2>
          </div>
          {recentSubs.length === 0 ? (
            <div className="px-6 py-12 text-center text-zinc-500 text-sm">
              No sub events recorded yet. Register your subscriptions above to start tracking.
            </div>
          ) : (
            <div className="divide-y divide-zinc-800">
              {recentSubs.map(sub => (
                <div key={sub.id} className="px-6 py-3 flex items-center justify-between">
                  <div>
                    <span className="text-sm text-white">{kindLabel(sub.kind, sub)}</span>
                  </div>
                  <div className="flex items-center gap-3 text-xs text-zinc-500">
                    <span className="bg-zinc-800 px-2 py-0.5 rounded">{tierLabel(sub.tier)}</span>
                    <span>{new Date(sub.occurredAt).toLocaleString()}</span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

      </main>
    </div>
  )
}
