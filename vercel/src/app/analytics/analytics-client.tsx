"use client"
import { useState, useCallback } from "react"
import Link from "next/link"
import {
  BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, Legend,
} from "recharts"
import { AppHeader } from "@/components/app-header"
import type { AnalyticsOverview, AnalyticsTotals, AnalyticsSession, DayBucket } from "@/services"

type Range = "7d" | "30d" | "90d"
type ChartTab = "activity" | "revenue"

const RANGE_LABELS: Record<Range, string> = { "7d": "7 days", "30d": "30 days", "90d": "90 days" }

const TOOLTIP_STYLE = {
  backgroundColor: "#18181b",
  border: "1px solid #3f3f46",
  borderRadius: 8,
  fontSize: 12,
  color: "#e4e4e7",
}

// Colours consistent with event type badges elsewhere
const COLORS = {
  follows:    "#3b82f6",
  subs:       "#a855f7",
  bits:       "#eab308",
  raids:      "#22c55e",
  superchats: "#ef4444",
  members:    "#f97316",
}

function formatDuration(minutes: number | null): string {
  if (minutes === null) return "Live"
  if (minutes < 60) return `${minutes}m`
  const h = Math.floor(minutes / 60)
  const m = minutes % 60
  return m === 0 ? `${h}h` : `${h}h ${m}m`
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, { month: "short", day: "numeric" })
}

function formatAxisDate(iso: string): string {
  const d = new Date(iso)
  return `${d.getMonth() + 1}/${d.getDate()}`
}

function formatCurrency(amount: number, currency: string): string {
  try {
    return new Intl.NumberFormat(undefined, { style: "currency", currency, maximumFractionDigits: 2 }).format(amount)
  } catch {
    return `${amount.toFixed(2)} ${currency}`
  }
}

// ── Summary cards ──────────────────────────────────────────────────────────────

function StatCard({ label, primary, secondary, color, active, dimmed, onClick }: {
  label: string
  primary: string
  secondary?: string
  color: string
  active?: boolean
  dimmed?: boolean
  onClick?: () => void
}) {
  return (
    <div
      onClick={onClick}
      className={`bg-white dark:bg-zinc-900 border rounded-xl px-5 py-4 flex flex-col justify-between gap-3 transition-all duration-150 ${
        onClick ? "cursor-pointer select-none" : ""
      } ${
        active
          ? "border-zinc-400 dark:border-zinc-500 ring-1 ring-zinc-300 dark:ring-zinc-600"
          : dimmed
          ? "border-zinc-200 dark:border-zinc-800 opacity-40"
          : "border-zinc-200 dark:border-zinc-800 hover:border-zinc-300 dark:hover:border-zinc-700"
      }`}
    >
      <div className="flex items-center gap-2">
        <span className="w-2 h-2 rounded-full shrink-0" style={{ background: color }} />
        <span className="text-xs text-zinc-500 dark:text-zinc-400">{label}</span>
      </div>
      <div>
        <p className="text-2xl font-bold leading-none">{primary}</p>
        {secondary && <p className="text-xs text-zinc-400 dark:text-zinc-500 mt-1">{secondary}</p>}
      </div>
    </div>
  )
}

function formatSuperchatTotal(byCurrency: Record<string, number>): string {
  const entries = Object.entries(byCurrency)
  if (entries.length === 0) return "—"
  if (entries.length === 1) return formatCurrency(entries[0][1], entries[0][0])
  // Multiple currencies: show largest amount + note
  const [topCur, topAmt] = entries.sort((a, b) => b[1] - a[1])[0]
  return `${formatCurrency(topAmt, topCur)} +${entries.length - 1} more`
}

type EventTypeKey = "follows" | "subs" | "bits" | "raids" | "superchats" | "members"

function TotalsGrid({
  totals, hasYouTube, selectedTypes, onToggle,
}: {
  totals: AnalyticsTotals
  hasYouTube: boolean
  selectedTypes: Set<EventTypeKey>
  onToggle: (key: EventTypeKey) => void
}) {
  const anySelected = selectedTypes.size > 0
  const isActive = (k: EventTypeKey) => selectedTypes.has(k)
  const isDimmed = (k: EventTypeKey) => anySelected && !selectedTypes.has(k)

  return (
    <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3">
      <StatCard
        label="Followers"
        primary={totals.follows.toLocaleString()}
        color={COLORS.follows}
        active={isActive("follows")} dimmed={isDimmed("follows")}
        onClick={() => onToggle("follows")}
      />
      <StatCard
        label="Subscribers"
        primary={totals.subs.toLocaleString()}
        color={COLORS.subs}
        active={isActive("subs")} dimmed={isDimmed("subs")}
        onClick={() => onToggle("subs")}
      />
      <StatCard
        label="Bits"
        primary={totals.bits.total.toLocaleString()}
        secondary={`${totals.bits.count} cheers`}
        color={COLORS.bits}
        active={isActive("bits")} dimmed={isDimmed("bits")}
        onClick={() => onToggle("bits")}
      />
      <StatCard
        label="Raid viewers"
        primary={totals.raids.total.toLocaleString()}
        secondary={`${totals.raids.count} raids`}
        color={COLORS.raids}
        active={isActive("raids")} dimmed={isDimmed("raids")}
        onClick={() => onToggle("raids")}
      />
      {hasYouTube && (
        <>
          <StatCard
            label="Super Chats"
            primary={formatSuperchatTotal(totals.superchats.byCurrency)}
            secondary={`${totals.superchats.count} superchats`}
            color={COLORS.superchats}
            active={isActive("superchats")} dimmed={isDimmed("superchats")}
            onClick={() => onToggle("superchats")}
          />
          <StatCard
            label="Members"
            primary={totals.members.toLocaleString()}
            color={COLORS.members}
            active={isActive("members")} dimmed={isDimmed("members")}
            onClick={() => onToggle("members")}
          />
        </>
      )}
    </div>
  )
}

// ── Chart ──────────────────────────────────────────────────────────────────────

function ActivityChart({ data, selected }: { data: DayBucket[]; selected: Set<EventTypeKey> }) {
  const show = (k: EventTypeKey) => selected.size === 0 || selected.has(k)
  return (
    <ResponsiveContainer width="100%" height={260}>
      <BarChart data={data} margin={{ top: 4, right: 8, left: -16, bottom: 0 }} barSize={6}>
        <XAxis dataKey="date" tickFormatter={formatAxisDate} tick={{ fontSize: 11 }} tickLine={false} axisLine={false} />
        <YAxis allowDecimals={false} tick={{ fontSize: 11 }} tickLine={false} axisLine={false} />
        <Tooltip labelFormatter={(v) => formatDate(v as string)} contentStyle={TOOLTIP_STYLE} cursor={{ fill: "rgba(161,161,170,0.08)" }} />
        <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 12 }} />
        {show("follows")    && <Bar dataKey="follows"        name="Follows"     fill={COLORS.follows}    stackId="a" radius={[0,0,0,0]} />}
        {show("subs")       && <Bar dataKey="subs"           name="Subs"        fill={COLORS.subs}       stackId="a" radius={[0,0,0,0]} />}
        {show("bits")       && <Bar dataKey="bitsCount"      name="Cheers"      fill={COLORS.bits}       stackId="a" radius={[0,0,0,0]} />}
        {show("raids")      && <Bar dataKey="raidsCount"     name="Raids"       fill={COLORS.raids}      stackId="a" radius={[0,0,0,0]} />}
        {show("superchats") && <Bar dataKey="superchatsCount" name="Superchats" fill={COLORS.superchats} stackId="a" radius={[0,0,0,0]} />}
        {show("members")    && <Bar dataKey="members"        name="Members"     fill={COLORS.members}    stackId="a" radius={[2,2,0,0]} />}
      </BarChart>
    </ResponsiveContainer>
  )
}

function RevenueChart({ data, selected }: { data: DayBucket[]; selected: Set<EventTypeKey> }) {
  const show = (k: EventTypeKey) => selected.size === 0 || selected.has(k)
  return (
    <ResponsiveContainer width="100%" height={260}>
      <BarChart data={data} margin={{ top: 4, right: 8, left: -16, bottom: 0 }} barSize={10} barCategoryGap="30%">
        <XAxis dataKey="date" tickFormatter={formatAxisDate} tick={{ fontSize: 11 }} tickLine={false} axisLine={false} />
        <YAxis allowDecimals={false} tick={{ fontSize: 11 }} tickLine={false} axisLine={false} />
        <Tooltip labelFormatter={(v) => formatDate(v as string)} contentStyle={TOOLTIP_STYLE} cursor={{ fill: "rgba(161,161,170,0.08)" }} />
        <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 12 }} />
        {show("bits")       && <Bar dataKey="bitsTotal"       name="Bits"        fill={COLORS.bits}       radius={[3,3,0,0]} />}
        {show("raids")      && <Bar dataKey="raidViewers"     name="Raid viewers" fill={COLORS.raids}     radius={[3,3,0,0]} />}
        {show("superchats") && <Bar dataKey="superchatsTotal" name="Super Chats" fill={COLORS.superchats} radius={[3,3,0,0]} />}
      </BarChart>
    </ResponsiveContainer>
  )
}

// ── Sessions table ─────────────────────────────────────────────────────────────

function SessionsTable({ sessions }: { sessions: AnalyticsSession[] }) {
  if (sessions.length === 0) {
    return (
      <div className="px-6 py-10 text-center text-sm text-zinc-500">
        No sessions recorded yet. Sessions are tracked automatically once your Twitch EventSub subscriptions are registered.
      </div>
    )
  }

  return (
    <div className="divide-y divide-zinc-200 dark:divide-zinc-800/60">
      {sessions.map(s => (
        <div key={s.id} className="px-6 py-3 flex items-center gap-4">
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium">
              {formatDate(s.startedAt)}
              <span className="text-zinc-400 dark:text-zinc-500 font-normal ml-2 tabular-nums">
                {new Date(s.startedAt).toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit" })}
                {" – "}
                {s.endedAt
                  ? new Date(s.endedAt).toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit" })
                  : "ongoing"}
                {" · "}
                {formatDuration(s.durationMinutes)}
              </span>
            </p>
            <p className="text-xs text-zinc-400 dark:text-zinc-500 mt-0.5 flex items-center gap-3">
              {s.summary.follows > 0  && <span><span style={{ color: COLORS.follows  }}>♥</span> {s.summary.follows} follows</span>}
              {s.summary.subs > 0     && <span><span style={{ color: COLORS.subs     }}>★</span> {s.summary.subs} subs</span>}
              {s.summary.bits > 0     && <span><span style={{ color: COLORS.bits     }}>◆</span> {s.summary.bits.toLocaleString()} bits</span>}
              {s.summary.raids > 0    && <span><span style={{ color: COLORS.raids    }}>▶</span> {s.summary.raids.toLocaleString()} viewers raided</span>}
              {s.summary.follows === 0 && s.summary.subs === 0 && s.summary.bits === 0 && s.summary.raids === 0 && (
                <span className="italic">No events</span>
              )}
            </p>
          </div>
          <Link
            href={`/analytics/${s.id}`}
            className="text-xs text-purple-500 hover:text-purple-400 transition-colors shrink-0"
          >
            View →
          </Link>
        </div>
      ))}
    </div>
  )
}

// ── Main component ─────────────────────────────────────────────────────────────

export function AnalyticsClient({
  initialData,
  initialRange,
  hasYouTube,
  displayName,
}: {
  initialData: AnalyticsOverview
  initialRange: Range
  hasYouTube: boolean
  displayName: string
}) {
  const [range, setRange] = useState<Range>(initialRange)
  const [chartTab, setChartTab] = useState<ChartTab>("activity")
  const [data, setData] = useState<AnalyticsOverview>(initialData)
  const [loading, setLoading] = useState(false)
  const [selectedTypes, setSelectedTypes] = useState<Set<EventTypeKey>>(new Set())

  function toggleType(key: EventTypeKey) {
    setSelectedTypes(prev => {
      const next = new Set(prev)
      if (next.has(key)) {
        next.delete(key)
        // if that was the last one, deselect all (show all)
      } else {
        next.add(key)
      }
      return next
    })
  }

  const fetchRange = useCallback(async (r: Range) => {
    setRange(r)
    setLoading(true)
    try {
      const res = await fetch(`/api/analytics?range=${r}`)
      if (res.ok) setData(await res.json())
    } finally {
      setLoading(false)
    }
  }, [])

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <AppHeader displayName={displayName} />

      <main className="max-w-5xl mx-auto px-6 py-10 space-y-6">

        {/* Header + range selector */}
        <div className="flex items-center justify-between">
          <h1 className="text-xl font-semibold tracking-tight">Analytics</h1>
          <div className="flex items-center gap-1 bg-zinc-100 dark:bg-zinc-800/60 rounded-lg p-1">
            {(["7d", "30d", "90d"] as Range[]).map(r => (
              <button
                key={r}
                onClick={() => fetchRange(r)}
                disabled={loading}
                className={`text-xs px-3 py-1.5 rounded-md font-medium transition-colors ${
                  range === r
                    ? "bg-white dark:bg-zinc-700 text-zinc-900 dark:text-white shadow-sm"
                    : "text-zinc-500 hover:text-zinc-700 dark:hover:text-zinc-300"
                }`}
              >
                {RANGE_LABELS[r]}
              </button>
            ))}
          </div>
        </div>

        {/* Totals */}
        <div className={loading ? "opacity-50 pointer-events-none transition-opacity" : "transition-opacity"}>
          <TotalsGrid totals={data.totals} hasYouTube={hasYouTube} selectedTypes={selectedTypes} onToggle={toggleType} />
        </div>

        {/* Chart */}
        <div className={`bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl overflow-hidden ${loading ? "opacity-50 pointer-events-none" : ""}`}>
          <div className="px-6 py-4 border-b border-zinc-200 dark:border-zinc-800 flex items-center justify-between">
            <h2 className="text-sm font-medium text-zinc-500 dark:text-zinc-400 uppercase tracking-wider">
              {chartTab === "activity" ? "Activity" : "Revenue"}
            </h2>
            <div className="flex items-center gap-1 bg-zinc-100 dark:bg-zinc-800/60 rounded-lg p-1">
              {(["activity", "revenue"] as ChartTab[]).map(t => (
                <button
                  key={t}
                  onClick={() => setChartTab(t)}
                  className={`text-xs px-3 py-1.5 rounded-md font-medium transition-colors capitalize ${
                    chartTab === t
                      ? "bg-white dark:bg-zinc-700 text-zinc-900 dark:text-white shadow-sm"
                      : "text-zinc-500 hover:text-zinc-700 dark:hover:text-zinc-300"
                  }`}
                >
                  {t}
                </button>
              ))}
            </div>
          </div>
          <div className="px-4 py-4">
            {chartTab === "activity"
              ? <ActivityChart data={data.byDay} selected={selectedTypes} />
              : <RevenueChart data={data.byDay} selected={selectedTypes} />
            }
          </div>
        </div>

        {/* Sessions */}
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl overflow-hidden">
          <div className="px-6 py-4 border-b border-zinc-200 dark:border-zinc-800">
            <h2 className="text-sm font-medium text-zinc-500 dark:text-zinc-400 uppercase tracking-wider">Stream Sessions</h2>
          </div>
          <SessionsTable sessions={data.sessions} />
        </div>

      </main>
    </div>
  )
}