"use client"
import { useState } from "react"
import type { LiveEvent, LiveEventType } from "@/types/events"

const ALL_TYPES: LiveEventType[] = ["follow", "sub", "bits", "raid", "superchat", "member"]

const TYPE_BADGE: Record<LiveEventType, string> = {
  sub:       "bg-purple-500/20 text-purple-400 border border-purple-500/40",
  follow:    "bg-blue-500/20 text-blue-400 border border-blue-500/40",
  bits:      "bg-yellow-500/20 text-yellow-500 border border-yellow-500/40",
  raid:      "bg-green-500/20 text-green-500 border border-green-500/40",
  superchat: "bg-red-500/20 text-red-400 border border-red-500/40",
  member:    "bg-orange-500/20 text-orange-400 border border-orange-500/40",
}

const TYPE_ICON: Record<LiveEventType, string> = {
  sub: "★", follow: "♥", bits: "◆", raid: "▶", superchat: "💬", member: "🎖",
}

const TYPE_FILTER_STYLE: Record<LiveEventType, string> = {
  sub:       "border-purple-500/40 text-purple-400 bg-purple-500/10",
  follow:    "border-blue-500/40 text-blue-400 bg-blue-500/10",
  bits:      "border-yellow-500/40 text-yellow-500 bg-yellow-500/10",
  raid:      "border-green-500/40 text-green-500 bg-green-500/10",
  superchat: "border-red-500/40 text-red-400 bg-red-500/10",
  member:    "border-orange-500/40 text-orange-400 bg-orange-500/10",
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

function formatRelativeTime(sessionStart: string, eventTime: string): string {
  const diffMs = new Date(eventTime).getTime() - new Date(sessionStart).getTime()
  if (diffMs < 0) return "+0:00"
  const totalSecs = Math.floor(diffMs / 1000)
  const h = Math.floor(totalSecs / 3600)
  const m = Math.floor((totalSecs % 3600) / 60)
  const s = totalSecs % 60
  if (h > 0) return `+${h}:${String(m).padStart(2, "0")}:${String(s).padStart(2, "0")}`
  return `+${m}:${String(s).padStart(2, "0")}`
}

export function SessionTimeline({
  events,
  sessionStart,
  presentTypes,
}: {
  events: LiveEvent[]
  sessionStart: string
  presentTypes: LiveEventType[]
}) {
  const [activeFilters, setActiveFilters] = useState<Set<LiveEventType>>(new Set())

  function toggleFilter(type: LiveEventType) {
    setActiveFilters(prev => {
      const next = new Set(prev)
      if (next.has(type)) next.delete(type)
      else next.add(type)
      return next
    })
  }

  const filtered = activeFilters.size === 0
    ? events
    : events.filter(e => activeFilters.has(e.type))

  return (
    <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl overflow-hidden">
      {/* Header + filters */}
      <div className="px-6 py-4 border-b border-zinc-200 dark:border-zinc-800 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-3">
          <h2 className="text-sm font-medium text-zinc-500 dark:text-zinc-400 uppercase tracking-wider">
            Event Timeline
          </h2>
          <span className="text-xs text-zinc-400 dark:text-zinc-500">
            {filtered.length}{activeFilters.size > 0 ? ` / ${events.length}` : ""} events
          </span>
        </div>

        {/* Type filter pills */}
        {presentTypes.length > 1 && (
          <div className="flex flex-wrap gap-1.5">
            {presentTypes.map(type => {
              const active = activeFilters.has(type)
              return (
                <button
                  key={type}
                  onClick={() => toggleFilter(type)}
                  className={`text-xs px-2.5 py-1 rounded-full border font-medium transition-all ${
                    active
                      ? TYPE_FILTER_STYLE[type]
                      : "border-zinc-200 dark:border-zinc-700 text-zinc-400 dark:text-zinc-500 hover:border-zinc-300 dark:hover:border-zinc-600"
                  }`}
                >
                  {TYPE_ICON[type]} {type}
                </button>
              )
            })}
            {activeFilters.size > 0 && (
              <button
                onClick={() => setActiveFilters(new Set())}
                className="text-xs px-2.5 py-1 rounded-full border border-zinc-200 dark:border-zinc-700 text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-300 transition-colors"
              >
                Clear
              </button>
            )}
          </div>
        )}
      </div>

      {/* Timeline rows */}
      {filtered.length === 0 ? (
        <div className="px-6 py-10 text-center text-sm text-zinc-500">
          No events match the selected filters.
        </div>
      ) : (
        <div className="divide-y divide-zinc-200 dark:divide-zinc-800/60">
          {filtered.map(event => (
            <div key={event.id} className="px-6 py-3 flex items-center gap-4">
              {/* Relative time */}
              <span className="text-xs text-zinc-400 dark:text-zinc-600 tabular-nums w-16 shrink-0">
                {formatRelativeTime(sessionStart, event.occurredAt)}
              </span>

              {/* Type badge */}
              <span className={`shrink-0 text-xs px-2 py-0.5 rounded font-medium ${TYPE_BADGE[event.type]}`}>
                {TYPE_ICON[event.type]} {event.type}
              </span>

              {/* User */}
              <span className="flex-1 text-sm truncate">{event.fromUser}</span>

              {/* Amount */}
              {event.amount !== null && (
                <span className="text-sm text-zinc-500 dark:text-zinc-400 shrink-0">
                  {formatAmount(event.type, event.amount, event.currency)}
                </span>
              )}

              {/* Wall clock time */}
              <span className="text-xs text-zinc-300 dark:text-zinc-700 shrink-0 tabular-nums">
                {new Date(event.occurredAt).toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit" })}
              </span>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
