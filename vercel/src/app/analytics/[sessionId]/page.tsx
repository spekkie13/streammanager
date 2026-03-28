import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { redirect, notFound } from "next/navigation"
import Link from "next/link"
import { analyticsService } from "@/services"
import { AppHeader } from "@/components/app-header"
import type { AnalyticsTotals } from "@/services"
import type { LiveEventType } from "@/types/events"
import { SessionTimeline } from "./session-timeline"
import {EVENT_COLORS} from "@/constants/colors";

function formatDuration(minutes: number | null): string {
  if (minutes === null) return "Ongoing"
  if (minutes < 60) return `${minutes}m`
  const h: number = Math.floor(minutes / 60)
  const m: number = minutes % 60
  return m === 0 ? `${h}h` : `${h}h ${m}m`
}

function formatCurrency(amount: number, currency: string): string {
  try {
    return new Intl.NumberFormat(undefined, { style: "currency", currency, maximumFractionDigits: 2 }).format(amount)
  } catch {
    return `${amount.toFixed(2)} ${currency}`
  }
}

function formatSuperchatTotal(byCurrency: Record<string, number>): string {
  const entries: [string, number][] = Object.entries(byCurrency)
  if (entries.length === 0) return "—"
  if (entries.length === 1) return formatCurrency(entries[0][1], entries[0][0])
  const [topCur, topAmt] = entries.sort((a, b) => b[1] - a[1])[0]
  return `${formatCurrency(topAmt, topCur)} +${entries.length - 1} more`
}

function StatCard({ label, primary, secondary, color }: {
  label: string
  primary: string
  secondary?: string
  color: string
}) {
  return (
    <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl px-5 py-4 flex flex-col justify-between gap-3">
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

function TotalsRow({ totals }: { totals: AnalyticsTotals }) {
  return (
    <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3">
      <StatCard
          label="Followers"
          primary={totals.follows.toLocaleString()}
          color={EVENT_COLORS.follow}
      />
      <StatCard
          label="Subscribers"
          primary={totals.subs.toLocaleString()}
          color={EVENT_COLORS.sub}
      />
      <StatCard
        label="Bits"
        primary={totals.bits.total.toLocaleString()}
        secondary={`${totals.bits.count} cheers`}
        color={EVENT_COLORS.bits}
      />
      <StatCard
        label="Raid viewers"
        primary={totals.raids.total.toLocaleString()}
        secondary={`${totals.raids.count} raids`}
        color={EVENT_COLORS.raid}
      />
      {totals.superchats.count > 0 && (
        <StatCard
          label="Super Chats"
          primary={formatSuperchatTotal(totals.superchats.byCurrency)}
          secondary={`${totals.superchats.count} superchats`}
          color={EVENT_COLORS.superchat}
        />
      )}
      {totals.members > 0 && (
        <StatCard label="Members" primary={totals.members.toLocaleString()} color={EVENT_COLORS.member} />
      )}
    </div>
  )
}

export default async function SessionDetailPage({
  params,
}: {
  params: { sessionId: string }
}) {
  const session = await getServerSession(authOptions)
  if (!session) redirect("/")
  if (!session.twitchId) redirect("/analytics")

  const detail = await analyticsService.getSessionDetail(params.sessionId, session.twitchId)
  if (!detail) notFound()

  const { session: s, totals, events } = detail
  const startDate = new Date(s.startedAt)
  const endDate = s.endedAt ? new Date(s.endedAt) : null

  const presentTypes = Array.from(new Set(events.map(e => e.type))) as LiveEventType[]

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <AppHeader displayName={session.displayName} />

      <main className="max-w-5xl mx-auto px-6 py-10 space-y-6">

        {/* Back link */}
        <Link href="/analytics" className="inline-block text-xs text-zinc-400 hover:text-zinc-700 dark:hover:text-zinc-300 transition-colors">
          ← Back to Analytics
        </Link>

        {/* Session header card */}
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl px-6 py-5">
          <h1 className="text-lg font-semibold tracking-tight">
            {startDate.toLocaleDateString(undefined, { weekday: "long", month: "long", day: "numeric" })}
          </h1>
          <p className="text-sm text-zinc-400 dark:text-zinc-500 tabular-nums mt-1">
            {startDate.toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit" })}
            {" – "}
            {endDate
              ? endDate.toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit" })
              : "ongoing"}
            {" · "}
            {formatDuration(s.durationMinutes)}
            {" · "}
            <span>{events.length} events</span>
          </p>
        </div>

        {/* Totals */}
        <TotalsRow totals={totals} />

        {/* Event timeline */}
        <SessionTimeline events={events} sessionStart={s.startedAt} presentTypes={presentTypes} />

      </main>
    </div>
  )
}
