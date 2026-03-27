"use client"
import { useState, useEffect, useRef, useCallback } from "react"
import { useSearchParams } from "next/navigation"
import type { LiveEvent } from "@/types/events"
import { formatAmount } from "@/lib/format"
import { TWITCH_TIER_LABEL } from "@/lib/event-types"

const ALERT_CONFIG: Record<string, { label: string; color: string }> = {
  sub:       { label: "NEW SUBSCRIBER", color: "#9146FF" },
  follow:    { label: "NEW FOLLOWER",   color: "#4299E1" },
  bits:      { label: "BITS CHEERED",   color: "#F59E0B" },
  raid:      { label: "INCOMING RAID",  color: "#22C55E" },
  superchat: { label: "SUPER CHAT",     color: "#EF4444" },
  member:    { label: "NEW MEMBER",     color: "#F97316" },
}

function alertSubtitle(event: LiveEvent): string | null {
  if (event.type === "sub") {
    if (event.subKind === "community_gift" && event.amount != null)
      return `Gifted ${event.amount} subscription${event.amount !== 1 ? "s" : ""}`
    if (event.subKind === "resub" && event.cumulativeMonths != null)
      return `${event.cumulativeMonths} months`
    return event.tier ? (TWITCH_TIER_LABEL[event.tier] ?? null) : null
  }
  if (event.type === "bits" || event.type === "raid")
    return formatAmount(event.type, event.amount, null)
  if (event.type === "superchat")
    return formatAmount("superchat", event.amount, event.currency)
  if (event.type === "member") {
    if (event.levelName) return event.levelName
    if (event.amount != null) return `${event.amount} month${event.amount !== 1 ? "s" : ""}`
  }
  return null
}

export default function AlertsWidget() {
  const params = useSearchParams()
  const token = params.get("token") ?? ""
  const holdDuration = (parseInt(params.get("duration") ?? "6")) * 1000
  const fontSize = parseInt(params.get("fontSize") ?? "0") || 18

  const queueRef = useRef<LiveEvent[]>([])
  const busyRef = useRef(false)
  const [current, setCurrent] = useState<LiveEvent | null>(null)
  const [visible, setVisible] = useState(false)

  useEffect(() => {
    document.documentElement.style.background = "transparent"
    document.body.style.background = "transparent"
  }, [])

  const showNext = useCallback(() => {
    if (busyRef.current || queueRef.current.length === 0) return
    const next = queueRef.current.shift()!
    busyRef.current = true
    setCurrent(next)
    setVisible(false)
    setTimeout(() => setVisible(true), 50)
  }, [])

  // SSE connection
  useEffect(() => {
    if (!token) return
    const es = new EventSource(`/api/widget/events/stream?token=${token}`)
    es.onmessage = (e: MessageEvent) => {
      const incoming: LiveEvent[] = JSON.parse(e.data as string)
      queueRef.current.push(...incoming)
      if (!busyRef.current) showNext()
    }
    es.onerror = () => es.close()
    return () => es.close()
  }, [token, showNext])

  // Auto-dismiss after hold duration, then show next
  useEffect(() => {
    if (!visible || !current) return
    const t = setTimeout(() => {
      setVisible(false)
      setTimeout(() => {
        setCurrent(null)
        busyRef.current = false
        showNext()
      }, 500)
    }, holdDuration)
    return () => clearTimeout(t)
  }, [visible, current, holdDuration, showNext])

  if (!current) return null

  const config = ALERT_CONFIG[current.type] ?? { label: current.type.toUpperCase(), color: "#6B7280" }
  const subtitle = alertSubtitle(current)

  return (
    <div className="fixed inset-0 flex items-end justify-center p-6 pointer-events-none select-none">
      <div
        style={{
          transform: visible ? "translateY(0)" : "translateY(2rem)",
          opacity: visible ? 1 : 0,
          transition: "transform 0.5s cubic-bezier(0.34, 1.56, 0.64, 1), opacity 0.4s ease",
          border: `1px solid ${config.color}`,
          boxShadow: `0 0 32px ${config.color}55, 0 8px 32px rgba(0,0,0,0.5)`,
          fontFamily: "Inter, system-ui, sans-serif",
          minWidth: "300px",
          maxWidth: "420px",
        }}
        className="relative rounded-2xl overflow-hidden"
      >
        {/* Backdrop */}
        <div className="absolute inset-0 bg-black/80 backdrop-blur-md" />

        {/* Content */}
        <div className="relative px-6 py-5 space-y-1.5">

          {/* Type label row */}
          <div className="flex items-center justify-between gap-3">
            <span
              className="font-bold tracking-widest uppercase"
              style={{ color: config.color, fontSize: `${Math.round(fontSize * 0.65)}px` }}
            >
              {config.label}
            </span>
            {current.isReplay && (
              <span
                className="text-[10px] font-bold tracking-wider uppercase px-1.5 py-0.5 rounded shrink-0"
                style={{ color: config.color, border: `1px solid ${config.color}66` }}
              >
                REPLAY
              </span>
            )}
          </div>

          {/* Username */}
          <p
            className="font-bold leading-tight text-white"
            style={{
              fontSize: `${Math.round(fontSize * 1.4)}px`,
              textShadow: "0 2px 8px rgba(0,0,0,0.8)",
            }}
          >
            {current.fromUser}
          </p>

          {/* Subtitle */}
          {subtitle && (
            <p
              className="font-medium"
              style={{ color: "rgba(255,255,255,0.7)", fontSize: `${fontSize}px` }}
            >
              {subtitle}
            </p>
          )}

          {/* Message */}
          {current.message && (
            <p
              className="leading-snug italic"
              style={{ color: "rgba(255,255,255,0.55)", fontSize: `${Math.round(fontSize * 0.85)}px` }}
            >
              &ldquo;{current.message}&rdquo;
            </p>
          )}
        </div>

        {/* Bottom accent bar */}
        <div className="h-1" style={{ background: config.color }} />
      </div>
    </div>
  )
}
