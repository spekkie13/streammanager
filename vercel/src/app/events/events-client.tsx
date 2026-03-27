"use client"
import { useState, useEffect, useCallback } from "react"
import type { LiveEvent, LiveEventType } from "@/types/events"
import type { EventSortBy, SortOrder, PaginatedEvents } from "@/types/event-filter"
import { AppHeader } from "@/components/app-header"
import { TwitchLogo, YouTubeLogo } from "@/components/platform-logos"
import { ReplayButton } from "@/components/replay-button"
import { EVENT_TYPES, TYPE_BADGE, TYPE_ICON, TWITCH_TIER_LABEL, SUB_KIND_LABEL, MODAL_TYPES } from "@/lib/event-types"
import { formatAmount, formatDateTime } from "@/lib/format"

function DetailRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between gap-4 py-2 border-b border-zinc-100 dark:border-zinc-800 last:border-0">
      <span className="text-xs text-zinc-500 dark:text-zinc-400 shrink-0">{label}</span>
      <span className="text-xs text-zinc-900 dark:text-white text-right">{value}</span>
    </div>
  )
}

function EventDetailModal({ event, onClose }: { event: LiveEvent; onClose: () => void }) {
  useEffect(() => {
    function onKey(e: KeyboardEvent) { if (e.key === "Escape") onClose() }
    window.addEventListener("keydown", onKey)
    return () => window.removeEventListener("keydown", onKey)
  }, [onClose])

  const title = event.type === "sub"
    ? SUB_KIND_LABEL[event.subKind ?? "new"] ?? "Subscription"
    : event.type === "bits" ? "Bits Cheered"
    : event.type === "superchat" ? "Super Chat"
    : "Membership"

  const formattedAmount = formatAmount(event.type, event.amount, event.currency)

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm"
      onClick={onClose}
    >
      <div
        className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl w-full max-w-sm shadow-xl"
        onClick={e => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex items-center justify-between px-5 py-4 border-b border-zinc-200 dark:border-zinc-800">
          <div className="flex items-center gap-2.5">
            {event.platform === "youtube"
              ? <YouTubeLogo className="w-3.5 h-3.5 text-[#FF0000]" />
              : <TwitchLogo className="w-3.5 h-3.5 text-[#9146FF]" />
            }
            <span className={`text-xs px-2 py-0.5 rounded font-medium ${TYPE_BADGE[event.type]}`}>
              {TYPE_ICON[event.type]} {event.type}
            </span>
            <span className="text-sm font-semibold">{title}</span>
          </div>
          <button
            onClick={onClose}
            className="text-zinc-400 hover:text-zinc-700 dark:hover:text-zinc-200 transition-colors text-lg leading-none"
            aria-label="Close"
          >
            ×
          </button>
        </div>

        {/* Body */}
        <div className="px-5 py-4 space-y-0">
          <DetailRow label="From" value={event.fromUser} />

          {event.type === "sub" && (
            <>
              {event.tier && <DetailRow label="Tier" value={TWITCH_TIER_LABEL[event.tier] ?? event.tier} />}
              {event.subKind === "resub" && event.cumulativeMonths != null && (
                <DetailRow label="Total months" value={`${event.cumulativeMonths} months`} />
              )}
              {event.subKind === "community_gift" && event.amount != null && (
                <DetailRow label="Gifted" value={`${event.amount} subscription${event.amount !== 1 ? "s" : ""}`} />
              )}
            </>
          )}

          {event.type === "bits" && formattedAmount && (
            <DetailRow label="Amount" value={formattedAmount} />
          )}

          {event.type === "superchat" && formattedAmount && (
            <DetailRow label="Amount" value={formattedAmount} />
          )}

          {event.type === "member" && (
            <>
              {event.amount != null && <DetailRow label="Member for" value={`${event.amount} month${event.amount !== 1 ? "s" : ""}`} />}
              {event.levelName && <DetailRow label="Level" value={event.levelName} />}
            </>
          )}

          {event.message && (
            <div className="py-3 border-b border-zinc-100 dark:border-zinc-800">
              <p className="text-xs text-zinc-500 dark:text-zinc-400 mb-1.5">Message</p>
              <p className="text-sm text-zinc-900 dark:text-white italic">&ldquo;{event.message}&rdquo;</p>
            </div>
          )}

          <DetailRow label="Time" value={new Date(event.occurredAt).toLocaleString(undefined, { dateStyle: "medium", timeStyle: "short" })} />
        </div>
      </div>
    </div>
  )
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
  const [selectedEvent, setSelectedEvent] = useState<LiveEvent | null>(null)

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
    <div className="min-h-screen">
      {selectedEvent && <EventDetailModal event={selectedEvent} onClose={() => setSelectedEvent(null)} />}
      <AppHeader displayName={displayName} />

      <main className="max-w-5xl mx-auto px-6 py-8 space-y-6">
        <div>
          <h1 className="text-2xl font-bold">Event History</h1>
          <p className="text-zinc-500 text-sm mt-1">Browse, filter and sort all events from your streams.</p>
        </div>

        {/* Filters */}
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl p-5 space-y-4">
          <h2 className="text-xs font-medium text-zinc-500 dark:text-zinc-400 uppercase tracking-wider">Filters</h2>

          {/* Type toggles */}
          <div className="flex flex-wrap gap-2">
            {EVENT_TYPES.map(t => (
              <button
                key={t.value}
                onClick={() => toggleType(t.value)}
                className={`px-3 py-1.5 rounded-lg text-sm font-medium border transition-all ${
                  activeTypes.has(t.value)
                    ? t.activeClass
                    : "bg-zinc-100 dark:bg-zinc-800 text-zinc-600 dark:text-zinc-400 border-zinc-300 dark:border-zinc-700 hover:border-zinc-400 dark:hover:border-zinc-500"
                }`}
              >
                {TYPE_ICON[t.value]} {t.label}
              </button>
            ))}
            {activeTypes.size > 0 && (
              <button
                onClick={() => { setActiveTypes(new Set()); setPage(1) }}
                className="px-3 py-1.5 rounded-lg text-sm text-zinc-500 hover:text-zinc-900 dark:hover:text-zinc-300 transition-colors"
              >
                Clear
              </button>
            )}
          </div>

          {/* Date range + sort */}
          <div className="flex flex-wrap gap-4 items-end">
            <div className="space-y-1">
              <label className="text-xs text-zinc-500 mr-2">From</label>
              <input
                type="datetime-local"
                value={from}
                onChange={e => { setFrom(e.target.value); setPage(1) }}
                className="bg-zinc-100 dark:bg-zinc-800 border border-zinc-300 dark:border-zinc-700 rounded-lg px-3 py-2 text-sm text-zinc-900 dark:text-zinc-300 focus:outline-none focus:border-purple-500"
              />
            </div>
            <div className="space-y-1">
              <label className="text-xs text-zinc-500 mr-2">To</label>
              <input
                type="datetime-local"
                value={to}
                onChange={e => { setTo(e.target.value); setPage(1) }}
                className="bg-zinc-100 dark:bg-zinc-800 border border-zinc-300 dark:border-zinc-700 rounded-lg px-3 py-2 text-sm text-zinc-900 dark:text-zinc-300 focus:outline-none focus:border-purple-500"
              />
            </div>
            <div className="space-y-1">
              <label className="text-xs text-zinc-500 mr-2">Sort by</label>
              <select
                value={sortBy}
                onChange={e => handleSortBy(e.target.value as EventSortBy)}
                className="bg-zinc-100 dark:bg-zinc-800 border border-zinc-300 dark:border-zinc-700 rounded-lg px-3 py-2 text-sm text-zinc-900 dark:text-zinc-300 focus:outline-none focus:border-purple-500"
              >
                <option value="occurredAt">Date</option>
                <option value="amount">Amount</option>
                <option value="fromUser">User</option>
              </select>
            </div>
            <button
              onClick={toggleSortOrder}
              className="bg-zinc-100 dark:bg-zinc-800 border border-zinc-300 dark:border-zinc-700 hover:border-zinc-400 dark:hover:border-zinc-500 rounded-lg px-3 py-2 text-sm text-zinc-700 dark:text-zinc-300 transition-colors"
            >
              {sortOrder === "desc" ? "↓ Newest first" : "↑ Oldest first"}
            </button>
          </div>
        </div>

        {/* Results */}
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl overflow-hidden">
          <div className="px-6 py-3 border-b border-zinc-200 dark:border-zinc-800 flex items-center justify-between">
            <span className="text-xs font-medium text-zinc-500 dark:text-zinc-400 uppercase tracking-wider">
              {data ? `${data.total.toLocaleString()} events` : "Events"}
            </span>
            {loading && <span className="text-xs text-zinc-400 dark:text-zinc-600 animate-pulse">Loading...</span>}
          </div>

          {!data || data.events.length === 0 ? (
            <div className="px-6 py-12 text-center text-zinc-500 text-sm">
              {loading ? "Loading events..." : "No events match your filters."}
            </div>
          ) : (
            <div className="divide-y divide-zinc-200 dark:divide-zinc-800/60">
              {data.events.map(event => {
                const hasDetail = MODAL_TYPES.has(event.type)
                return (
                  <div
                    key={event.id}
                    onClick={() => hasDetail && setSelectedEvent(event)}
                    className={`px-6 py-3 flex items-center gap-4 ${hasDetail ? "cursor-pointer hover:bg-zinc-50 dark:hover:bg-zinc-800/50 transition-colors" : ""}`}
                  >
                    {event.platform === "youtube"
                      ? <YouTubeLogo className="shrink-0 w-3 h-3 text-[#FF0000]" />
                      : <TwitchLogo className="shrink-0 w-3 h-3 text-[#9146FF]" />
                    }
                    <span className={`shrink-0 text-xs px-2 py-0.5 rounded font-medium ${TYPE_BADGE[event.type]}`}>
                      {TYPE_ICON[event.type]} {event.type}
                    </span>
                    <span className="flex-1 text-sm text-zinc-900 dark:text-white truncate">
                      {event.fromUser}
                    </span>
                    {event.amount !== null && (
                      <span className="text-sm text-zinc-500 dark:text-zinc-400 shrink-0">
                        {formatAmount(event.type, event.amount, event.currency)}
                      </span>
                    )}
                    <span className="text-xs text-zinc-400 dark:text-zinc-600 shrink-0 w-36 text-right">
                      {formatDateTime(event.occurredAt)}
                    </span>
                    <ReplayButton event={event} />
                  </div>
                )
              })}
            </div>
          )}

          {data && data.totalPages > 1 && (
            <div className="px-6 py-4 border-t border-zinc-200 dark:border-zinc-800 flex items-center justify-between">
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page === 1}
                className="text-sm text-zinc-500 hover:text-zinc-900 dark:hover:text-white disabled:text-zinc-300 dark:disabled:text-zinc-700 disabled:cursor-not-allowed transition-colors"
              >
                ← Previous
              </button>
              <span className="text-xs text-zinc-500">
                Page {data.page} of {data.totalPages}
              </span>
              <button
                onClick={() => setPage(p => Math.min(data.totalPages, p + 1))}
                disabled={page === data.totalPages}
                className="text-sm text-zinc-500 hover:text-zinc-900 dark:hover:text-white disabled:text-zinc-300 dark:disabled:text-zinc-700 disabled:cursor-not-allowed transition-colors"
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