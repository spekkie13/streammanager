"use client"
import { AppHeader } from "@/components/app-header"
import { TYPE_BADGE, TYPE_ICON } from "@/lib/event-types"
import type { LiveEvent } from "@/types/events"
import type { StreamInfo } from "./page"

type SubGoal = { goal: number; initialCount: number; endsAt: string | null }
type SimpleGoal = { goal: number }

type Props = {
  displayName: string
  hasYouTube: boolean
  streamInfo: StreamInfo
  initialEvents: LiveEvent[]
  subGoal: SubGoal | null
  followGoal: SimpleGoal | null
  followTotal: number
  ytMemberGoal: SimpleGoal | null
  ytMemberTotal: number
}

function GoalBar({ label, current, goal }: { label: string; current: number; goal: number }) {
  const pct = Math.min(100, Math.round((current / goal) * 100))
  return (
    <div className="space-y-1">
      <div className="flex justify-between text-xs text-zinc-500 dark:text-zinc-400">
        <span>{label}</span>
        <span>{current} / {goal}</span>
      </div>
      <div className="h-1.5 rounded-full bg-zinc-200 dark:bg-zinc-700 overflow-hidden">
        <div className="h-full rounded-full bg-purple-500 transition-all" style={{ width: `${pct}%` }} />
      </div>
    </div>
  )
}

function LiveBadge({ isLive }: { isLive: boolean }) {
  if (!isLive) return (
    <span className="flex items-center gap-1.5 text-xs font-medium text-zinc-400 dark:text-zinc-500">
      <span className="w-2 h-2 rounded-full bg-zinc-400 dark:bg-zinc-600" />
      Offline
    </span>
  )
  return (
    <span className="flex items-center gap-1.5 text-xs font-semibold text-red-500">
      <span className="w-2 h-2 rounded-full bg-red-500 animate-pulse" />
      Live
    </span>
  )
}

function Uptime({ startedAt }: { startedAt: string }) {
  const started = new Date(startedAt)
  const now = new Date()
  const mins = Math.floor((now.getTime() - started.getTime()) / 60000)
  const h = Math.floor(mins / 60)
  const m = mins % 60
  return <span className="text-xs text-zinc-500 dark:text-zinc-400">{h}h {m}m</span>
}

export function LiveClient({
  displayName, hasYouTube, streamInfo, initialEvents,
  subGoal, followGoal, followTotal, ytMemberGoal, ytMemberTotal,
}: Props) {
  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950 flex flex-col">
      <AppHeader displayName={displayName} />

      {/* Status bar */}
      <div className="border-b border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 px-6 py-2.5 flex items-center gap-4">
        <LiveBadge isLive={streamInfo.isLive} />
        {streamInfo.isLive && streamInfo.startedAt && <Uptime startedAt={streamInfo.startedAt} />}
        {streamInfo.viewerCount !== null && (
          <span className="text-xs text-zinc-500 dark:text-zinc-400">
            {streamInfo.viewerCount.toLocaleString()} viewers
          </span>
        )}
        {!streamInfo.isLive && (
          <span className="text-xs text-zinc-400 dark:text-zinc-600">
            Start streaming on Twitch to see live stats here
          </span>
        )}
      </div>

      {/* Main layout */}
      <div className="flex-1 flex overflow-hidden">

        {/* Left — Chat (stub) */}
        <div className="flex-1 flex flex-col border-r border-zinc-200 dark:border-zinc-800 min-h-0">
          <div className="px-4 py-3 border-b border-zinc-200 dark:border-zinc-800 flex items-center gap-2">
            <h2 className="text-xs font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wider">
              Unified Chat
            </h2>
            <div className="flex items-center gap-1.5 ml-1">
              <span className="text-[10px] px-1.5 py-0.5 rounded bg-purple-500/15 text-purple-400 font-medium">Twitch</span>
              {hasYouTube && <span className="text-[10px] px-1.5 py-0.5 rounded bg-red-500/15 text-red-400 font-medium">YouTube</span>}
            </div>
          </div>
          <div className="flex-1 flex flex-col items-center justify-center gap-3 p-8 text-center">
            <div className="w-10 h-10 rounded-full bg-zinc-100 dark:bg-zinc-800 flex items-center justify-center text-lg">
              💬
            </div>
            <div className="space-y-1">
              <p className="text-sm font-medium text-zinc-600 dark:text-zinc-400">Unified chat coming soon</p>
              <p className="text-xs text-zinc-400 dark:text-zinc-600">
                Twitch {hasYouTube ? "and YouTube " : ""}chat will appear here in real time
              </p>
            </div>
          </div>
        </div>

        {/* Right sidebar */}
        <div className="w-80 shrink-0 flex flex-col overflow-y-auto divide-y divide-zinc-200 dark:divide-zinc-800">

          {/* Stream info */}
          <div className="p-4 space-y-3">
            <h2 className="text-xs font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wider">Stream Info</h2>
            {streamInfo.isLive ? (
              <div className="space-y-2">
                <div>
                  <p className="text-[10px] text-zinc-400 dark:text-zinc-600 uppercase tracking-wider mb-0.5">Title</p>
                  <p className="text-sm text-zinc-800 dark:text-zinc-200 leading-snug">
                    {streamInfo.title ?? <span className="italic text-zinc-400">No title</span>}
                  </p>
                </div>
                <div>
                  <p className="text-[10px] text-zinc-400 dark:text-zinc-600 uppercase tracking-wider mb-0.5">Category</p>
                  <p className="text-sm text-zinc-800 dark:text-zinc-200">
                    {streamInfo.category ?? <span className="italic text-zinc-400">No category</span>}
                  </p>
                </div>
              </div>
            ) : (
              <p className="text-xs text-zinc-400 dark:text-zinc-600 italic">No active stream</p>
            )}
          </div>

          {/* Recent events */}
          <div className="p-4 space-y-2 flex-1">
            <h2 className="text-xs font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wider">Recent Events</h2>
            {initialEvents.length === 0 ? (
              <p className="text-xs text-zinc-400 dark:text-zinc-600 italic">No events yet</p>
            ) : (
              <div className="space-y-1.5">
                {initialEvents.map(event => (
                  <div key={event.id} className="flex items-center gap-2">
                    <span className={`text-[10px] px-1.5 py-0.5 rounded font-medium shrink-0 ${TYPE_BADGE[event.type]}`}>
                      {TYPE_ICON[event.type]}
                    </span>
                    <span className="text-xs text-zinc-700 dark:text-zinc-300 truncate flex-1">{event.fromUser}</span>
                    <span className="text-[10px] text-zinc-400 dark:text-zinc-600 shrink-0 tabular-nums">
                      {new Date(event.occurredAt).toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit" })}
                    </span>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Goals */}
          <div className="p-4 space-y-3">
            <h2 className="text-xs font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wider">Goals</h2>
            {!subGoal && !followGoal && !ytMemberGoal ? (
              <p className="text-xs text-zinc-400 dark:text-zinc-600 italic">No active goals</p>
            ) : (
              <div className="space-y-3">
                {subGoal && (
                  <GoalBar label="Subscribers" current={0} goal={subGoal.goal} />
                )}
                {followGoal && (
                  <GoalBar label="Followers" current={followTotal} goal={followGoal.goal} />
                )}
                {ytMemberGoal && (
                  <GoalBar label="YT Members" current={ytMemberTotal} goal={ytMemberGoal.goal} />
                )}
              </div>
            )}
          </div>

        </div>
      </div>
    </div>
  )
}
