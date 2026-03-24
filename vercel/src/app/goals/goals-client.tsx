"use client"
import { useState } from "react"
import Link from "next/link"

type Props = {
  goal: number
  initialCount: number
  endsAt: string | null
  total: number
}

function toDateInputValue(iso: string | null): string {
  if (!iso) return ""
  return iso.slice(0, 10)
}

export function GoalsClient({ goal, initialCount, endsAt, total }: Props) {
  const [currentGoal, setCurrentGoal] = useState(goal)
  const [goalInput, setGoalInput] = useState(String(goal))
  const [initialCountInput, setInitialCountInput] = useState(String(initialCount))
  const [endsAtInput, setEndsAtInput] = useState(toDateInputValue(endsAt))
  const [savingGoal, setSavingGoal] = useState(false)
  const [saved, setSaved] = useState(false)

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
    setSaved(true)
    setTimeout(() => setSaved(false), 2000)
  }

  return (
    <div className="space-y-6">
      {/* Progress */}
      <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl p-6 space-y-4">
        <h2 className="text-sm font-medium text-zinc-500 dark:text-zinc-400 uppercase tracking-wider">Twitch — Sub Goal</h2>
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
        {endsAtInput && (
          <p className="text-xs text-zinc-400">
            Ends {new Date(endsAtInput).toLocaleDateString(undefined, { month: "long", day: "numeric", year: "numeric" })}
          </p>
        )}
      </div>

      {/* Edit form */}
      <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl p-6 space-y-4">
        <h2 className="text-sm font-medium text-zinc-500 dark:text-zinc-400 uppercase tracking-wider">Edit Goal</h2>
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
            <p className="text-xs text-zinc-400">Subs before CreatorDeck was set up</p>
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
          {saved && <span className="text-xs text-green-500">Saved</span>}
        </div>
      </div>

      <p className="text-xs text-zinc-400 text-center">
        <Link href="/dashboard" className="hover:text-zinc-600 dark:hover:text-zinc-300 transition-colors">
          ← Back to dashboard
        </Link>
      </p>
    </div>
  )
}
