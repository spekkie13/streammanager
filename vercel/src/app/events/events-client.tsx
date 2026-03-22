"use client"
import { useState, useEffect, useCallback } from "react"
import type { LiveEventType } from "@/types/events"
import type { EventSortBy, SortOrder, PaginatedEvents } from "@/types/event-filter"
import { AppHeader } from "@/components/app-header"

const EVENT_TYPES: { value: LiveEventType; label: string; color: string }[] = [
  { value: "sub",    label: "Subs",      color: "bg-purple-500/20 text-purple-300 border-purple-500/40" },
  { value: "follow", label: "Follows",   color: "bg-blue-500/20 text-blue-300 border-blue-500/40" },
  { value: "bits",   label: "Bits",      color: "bg-yellow-500/20 text-yellow-300 border-yellow-500/40" },
  { value: "raid",   label: "Raids",     color: "bg-green-500/20 text-green-300 border-green-500/40" },
]

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

function formatDate(iso: string) {
  return new Date(iso).toLocaleString(undefined, {
    month: "short", day: "numeric",
    hour: "2-digit", minute: "2-digit",
  })
}

export function EventsClient({ displayName }: { displayName: string }) {
  const [activeTypes, setActiveTypes] = useState<Set<LiveEventType>>(new Set())
  const [from, setFrom] = useState("")
  const [to, setTo] = useState("")
  const [sortBy, setSortBy] = useState<EventSortBy>("occurredAt")
  const [sortOrder, setSortOrder] = useState<SortOrder>("desc")
  const [page, setPage] = useState(1)

  const [data, setData] = useState<PaginatedEvents | null>(null)
  const [loading, setLoading] = useState(false)

  const fetchEvents = useCallback(async () => {
    setLoading(true)
    const params = new URLSearchParams()
    if (activeTypes.size > 0) params.set("types", Array.from(activeTypes).join(","))
    if (from) params.set("from", new Date(from).toISOString())
    if (to) params.set("to", new Date(to).toISOString())
    params.set("sortBy", sortBy)
    params.set("sortOrder", sortOrder)
    params.set("page", String(page))
    params.set("limit", "25")

    const res = await fetch(`/api/events?${params}`)
    if (res.ok) setData(await res.json())
    setLoading(false)
  }, [activeTypes, from, to, sortBy, sortOrder, page])

  useEffect(() => { fetchEvents() }, [fetchEvents])

  function toggleType(type: LiveEventType) {
    setActiveTypes(prev => {
      const next = new Set(prev)
      next.has(type) ? next.delete(type) : next.add(type)
      return next
    })
    setPage(1)
  }

  function handleSortBy(val: EventSortBy) {
    setSortBy(val)
    setPage(1)
  }

  function toggleSortOrder() {
    setSortOrder(o => o === "desc" ? "asc" : "desc")
    setPage(1)
  }

  return (
    <div className="min-h-screen bg-[#0a0a0a] text-white">
      <AppHeader displayName={displayName} />

      <main className="max-w-5xl mx-auto px-6 py-8 space-y-6">

        {/* Filters */}
        <div className="bg-zinc-900 border border-zinc-800 rounded-xl p-5 space-y-4">
          <h2 className="text-xs font-medium text-zinc-400 uppercase tracking-wider">Filters</h2>

          {/* Type toggles */}
          <div className="flex flex-wrap gap-2">
            {EVENT_TYPES.map(t => (
              <button
                key={t.value}
                onClick={() => toggleType(t.value)}
                className={`px-3 py-1.5 rounded-lg text-sm font-medium border transition-all ${
                  activeTypes.has(t.value)
                    ? t.color + " border-current"
                    : "bg-zinc-800 text-zinc-400 border-zinc-700 hover:border-zinc-500"
                }`}
              >
                {TYPE_ICON[t.value]} {t.label}
              </button>
            ))}
            {activeTypes.size > 0 && (
              <button
                onClick={() => { setActiveTypes(new Set()); setPage(1) }}
                className="px-3 py-1.5 rounded-lg text-sm text-zinc-500 hover:text-zinc-300 transition-colors"
              >
                Clear
              </button>
            )}
          </div>

          {/* Date range + sort */}
          <div className="flex flex-wrap gap-4 items-end">
            <div className="space-y-1">
              <label className="text-xs text-zinc-500">From</label>
              <input
                type="datetime-local"
                value={from}
                onChange={e => { setFrom(e.target.value); setPage(1) }}
                className="bg-zinc-800 border border-zinc-700 rounded-lg px-3 py-2 text-sm text-zinc-300 focus:outline-none focus:border-purple-500"
              />
            </div>
            <div className="space-y-1">
              <label className="text-xs text-zinc-500">To</label>
              <input
                type="datetime-local"
                value={to}
                onChange={e => { setTo(e.target.value); setPage(1) }}
                className="bg-zinc-800 border border-zinc-700 rounded-lg px-3 py-2 text-sm text-zinc-300 focus:outline-none focus:border-purple-500"
              />
            </div>
            <div className="space-y-1">
              <label className="text-xs text-zinc-500">Sort by</label>
              <select
                value={sortBy}
                onChange={e => handleSortBy(e.target.value as EventSortBy)}
                className="bg-zinc-800 border border-zinc-700 rounded-lg px-3 py-2 text-sm text-zinc-300 focus:outline-none focus:border-purple-500"
              >
                <option value="occurredAt">Date</option>
                <option value="amount">Amount</option>
                <option value="fromUser">User</option>
              </select>
            </div>
            <button
              onClick={toggleSortOrder}
              className="bg-zinc-800 border border-zinc-700 hover:border-zinc-500 rounded-lg px-3 py-2 text-sm text-zinc-300 transition-colors"
              title="Toggle sort order"
            >
              {sortOrder === "desc" ? "↓ Newest first" : "↑ Oldest first"}
            </button>
          </div>
        </div>

        {/* Results */}
        <div className="bg-zinc-900 border border-zinc-800 rounded-xl overflow-hidden">
          {/* Table header */}
          <div className="px-6 py-3 border-b border-zinc-800 flex items-center justify-between">
            <span className="text-xs font-medium text-zinc-400 uppercase tracking-wider">
              {data ? `${data.total.toLocaleString()} events` : "Events"}
            </span>
            {loading && <span className="text-xs text-zinc-600 animate-pulse">Loading...</span>}
          </div>

          {/* Rows */}
          {!data || data.events.length === 0 ? (
            <div className="px-6 py-12 text-center text-zinc-500 text-sm">
              {loading ? "Loading events..." : "No events match your filters."}
            </div>
          ) : (
            <div className="divide-y divide-zinc-800/60">
              {data.events.map(event => (
                <div key={event.id} className="px-6 py-3 flex items-center gap-4">
                  {/* Type badge */}
                  <span className={`shrink-0 text-xs px-2 py-0.5 rounded font-medium ${TYPE_BADGE[event.type]}`}>
                    {TYPE_ICON[event.type]} {event.type}
                  </span>

                  {/* User */}
                  <span className="flex-1 text-sm text-white truncate">
                    {event.fromUser}
                  </span>

                  {/* Amount */}
                  {event.amount !== null && (
                    <span className="text-sm text-zinc-400 shrink-0">
                      {formatAmount(event.type, event.amount)}
                    </span>
                  )}

                  {/* Date */}
                  <span className="text-xs text-zinc-600 shrink-0 w-36 text-right">
                    {formatDate(event.occurredAt)}
                  </span>
                </div>
              ))}
            </div>
          )}

          {/* Pagination */}
          {data && data.totalPages > 1 && (
            <div className="px-6 py-4 border-t border-zinc-800 flex items-center justify-between">
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page === 1}
                className="text-sm text-zinc-400 hover:text-white disabled:text-zinc-700 disabled:cursor-not-allowed transition-colors"
              >
                ← Previous
              </button>
              <span className="text-xs text-zinc-500">
                Page {data.page} of {data.totalPages}
              </span>
              <button
                onClick={() => setPage(p => Math.min(data.totalPages, p + 1))}
                disabled={page === data.totalPages}
                className="text-sm text-zinc-400 hover:text-white disabled:text-zinc-700 disabled:cursor-not-allowed transition-colors"
              >
                Next →
              </button>
            </div>
          )}
        </div>

      </main>
    </div>
  )
}
