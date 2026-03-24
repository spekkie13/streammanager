"use client"
import { useState } from "react"
import Link from "next/link"

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

function toDateInputValue(iso: string | null): string {
  if (!iso) return ""
  return iso.slice(0, 10)
}

type GoalCardProps = {
  label: string
  logo: React.ReactNode
  total: number
  savedGoal: number | null  // null = not set yet
  endsAt: string | null
  accentColor: string       // tailwind colour for progress bar, e.g. "bg-purple-500"
  apiType?: string          // undefined = uses /api/goal (Twitch subs), otherwise /api/goals
  initialCount?: number     // only for Twitch subs
}

function GoalCard({ label, logo, total, savedGoal, endsAt, accentColor, apiType, initialCount = 0 }: GoalCardProps) {
  const defaultGoal = savedGoal ?? 100
  const [currentGoal, setCurrentGoal] = useState(defaultGoal)
  const [goalInput, setGoalInput] = useState(String(defaultGoal))
  const [initialCountInput, setInitialCountInput] = useState(String(initialCount))
  const [endsAtInput, setEndsAtInput] = useState(toDateInputValue(endsAt))
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)

  const effectiveInitialCount = apiType ? 0 : (parseInt(initialCountInput) || 0)
  const displayTotal = total + effectiveInitialCount
  const progress = Math.min((displayTotal / currentGoal) * 100, 100)

  async function save() {
    const val = parseInt(goalInput)
    if (isNaN(val) || val < 1) return
    setSaving(true)
    if (apiType) {
      await fetch("/api/goals", {
        method: "POST",
        body: JSON.stringify({ type: apiType, goal: val, endsAt: endsAtInput || null }),
        headers: { "Content-Type": "application/json" },
      })
    } else {
      const initialCountVal = Math.max(0, parseInt(initialCountInput) || 0)
      await fetch("/api/goal", {
        method: "POST",
        body: JSON.stringify({ goal: val, initialCount: initialCountVal, endsAt: endsAtInput || null }),
        headers: { "Content-Type": "application/json" },
      })
    }
    setCurrentGoal(val)
    setSaving(false)
    setSaved(true)
    setTimeout(() => setSaved(false), 2000)
  }

  return (
    <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl p-6 space-y-4">
      {/* Header */}
      <div className="flex items-center gap-2">
        {logo}
        <h2 className="text-sm font-medium text-zinc-500 dark:text-zinc-400 uppercase tracking-wider">{label}</h2>
      </div>

      {/* Progress */}
      <div className="flex items-end gap-3">
        <span className="text-5xl font-bold">{displayTotal.toLocaleString()}</span>
        <span className="text-2xl text-zinc-400 dark:text-zinc-500 pb-1">/ {currentGoal.toLocaleString()}</span>
      </div>
      <div className="w-full bg-zinc-200 dark:bg-zinc-800 rounded-full h-3">
        <div className={`${accentColor} h-3 rounded-full transition-all duration-500`} style={{ width: `${progress}%` }} />
      </div>
      <p className="text-zinc-500 text-sm">{progress.toFixed(1)}% of goal reached</p>
      {endsAtInput && (
        <p className="text-xs text-zinc-400">
          Ends {new Date(endsAtInput).toLocaleDateString(undefined, { month: "long", day: "numeric", year: "numeric" })}
        </p>
      )}

      {/* Edit */}
      <div className="border-t border-zinc-100 dark:border-zinc-800 pt-4 space-y-3">
        <div className={`grid gap-4 ${!apiType ? "grid-cols-1 sm:grid-cols-3" : "grid-cols-1 sm:grid-cols-2"}`}>
          {!apiType && (
            <div className="flex flex-col gap-1.5">
              <label className="text-xs font-medium text-zinc-500 dark:text-zinc-400">Initial amount</label>
              <input
                type="number"
                value={initialCountInput}
                onChange={e => setInitialCountInput(e.target.value)}
                className="bg-zinc-100 dark:bg-zinc-800 border border-zinc-300 dark:border-zinc-700 rounded-lg px-3 py-2 text-sm focus:outline-none focus:border-purple-500 text-zinc-900 dark:text-white"
                min={0}
              />
              <p className="text-xs text-zinc-400">Subs before CreatorDeck</p>
            </div>
          )}
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
              End date <span className="font-normal text-zinc-400 dark:text-zinc-600">(optional)</span>
            </label>
            <div className="flex items-center gap-2">
              <input
                type="date"
                value={endsAtInput}
                onChange={e => setEndsAtInput(e.target.value)}
                className="flex-1 bg-zinc-100 dark:bg-zinc-800 border border-zinc-300 dark:border-zinc-700 rounded-lg px-3 py-2 text-sm focus:outline-none focus:border-purple-500 text-zinc-900 dark:text-white"
              />
              {endsAtInput && (
                <button onClick={() => setEndsAtInput("")} className="text-xs text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-300 transition-colors shrink-0">
                  Clear
                </button>
              )}
            </div>
          </div>
        </div>
        <div className="flex items-center gap-3">
          <button
            onClick={save}
            disabled={saving}
            className="bg-purple-500 hover:bg-purple-600 disabled:opacity-50 text-white text-sm font-medium px-5 py-2 rounded-lg transition-colors"
          >
            {saving ? "Saving..." : "Save"}
          </button>
          {saved && <span className="text-xs text-green-500">Saved</span>}
        </div>
      </div>
    </div>
  )
}

type Props = {
  subGoal: number
  subInitialCount: number
  subEndsAt: string | null
  subTotal: number
  hasTwitch: boolean
  hasYouTube: boolean
  followTotal: number
  followGoal: number | null
  followEndsAt: string | null
  ytMemberTotal: number
  ytMemberGoal: number | null
  ytMemberEndsAt: string | null
}

export function GoalsClient({
  subGoal, subInitialCount, subEndsAt, subTotal,
  hasTwitch, hasYouTube,
  followTotal, followGoal, followEndsAt,
  ytMemberTotal, ytMemberGoal, ytMemberEndsAt,
}: Props) {
  return (
    <div className="space-y-6">
      {hasTwitch && (
        <GoalCard
          label="Twitch — Subscribers"
          logo={<TwitchLogo className="w-4 h-4 text-[#9146FF]" />}
          total={subTotal}
          savedGoal={subGoal}
          endsAt={subEndsAt}
          accentColor="bg-purple-500"
          initialCount={subInitialCount}
        />
      )}

      {hasTwitch && (
        <GoalCard
          label="Twitch — Followers"
          logo={<TwitchLogo className="w-4 h-4 text-[#9146FF]" />}
          total={followTotal}
          savedGoal={followGoal}
          endsAt={followEndsAt}
          accentColor="bg-blue-500"
          apiType="twitch_follow"
        />
      )}

      {hasYouTube && (
        <GoalCard
          label="YouTube — Members"
          logo={<YouTubeLogo className="w-4 h-4 text-[#FF0000]" />}
          total={ytMemberTotal}
          savedGoal={ytMemberGoal}
          endsAt={ytMemberEndsAt}
          accentColor="bg-red-500"
          apiType="youtube_member"
        />
      )}

      {!hasYouTube && (
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl p-6">
          <div className="flex items-center gap-2 mb-2">
            <YouTubeLogo className="w-4 h-4 text-[#FF0000]" />
            <h2 className="text-sm font-medium text-zinc-500 dark:text-zinc-400 uppercase tracking-wider">YouTube — Members</h2>
          </div>
          <p className="text-sm text-zinc-500">
            Connect your YouTube account to track membership goals.{" "}
            <Link href="/connections" className="text-purple-500 hover:text-purple-400">Go to Connections →</Link>
          </p>
        </div>
      )}

      <p className="text-xs text-zinc-400 text-center">
        <Link href="/dashboard" className="hover:text-zinc-600 dark:hover:text-zinc-300 transition-colors">
          ← Back to dashboard
        </Link>
      </p>
    </div>
  )
}
