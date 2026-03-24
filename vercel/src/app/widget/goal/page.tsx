"use client"
import { useEffect, useState, useCallback } from "react"
import { useSearchParams } from "next/navigation"

type GoalData = {
  current: number
  goal: number
  label: string
  platform: "twitch" | "youtube"
}

const PLATFORM_COLOR: Record<string, string> = {
  twitch: "#9146FF",
  youtube: "#FF0000",
}

export default function GoalWidget() {
  const params = useSearchParams()
  const token = params.get("token") ?? ""
  const type = params.get("type") ?? "twitch_sub"
  const customLabel = params.get("label") ?? ""
  const barColor = params.get("color") ?? ""
  const fontSize = parseInt(params.get("fontSize") ?? "0") || 16

  const [data, setData] = useState<GoalData | null>(null)
  const [error, setError] = useState(false)

  const poll = useCallback(async () => {
    if (!token) return
    try {
      const res = await fetch(`/api/widget/goal?token=${token}&type=${type}`)
      if (!res.ok) { setError(true); return }
      setData(await res.json())
      setError(false)
    } catch {
      setError(true)
    }
  }, [token, type])

  useEffect(() => {
    poll()
    const id = setInterval(poll, 5000)
    return () => clearInterval(id)
  }, [poll])

  if (!token || error) return null
  if (!data) return null

  const progress = Math.min((data.current / data.goal) * 100, 100)
  const label = customLabel || data.label
  const color = barColor || PLATFORM_COLOR[data.platform] || "#9146FF"

  return (
    <div
      className="p-4 select-none"
      style={{ fontFamily: "Inter, sans-serif", fontSize: `${fontSize}px` }}
    >
      {/* Label + count */}
      <div className="flex items-baseline justify-between gap-4 mb-2">
        <span
          className="font-semibold tracking-tight"
          style={{ fontSize: `${fontSize}px`, color: "#fff", textShadow: "0 1px 4px rgba(0,0,0,0.8)" }}
        >
          {label}
        </span>
        <span
          className="font-bold tabular-nums"
          style={{ fontSize: `${Math.round(fontSize * 1.25)}px`, color: "#fff", textShadow: "0 1px 4px rgba(0,0,0,0.8)" }}
        >
          {data.current.toLocaleString()} <span style={{ opacity: 0.6, fontWeight: 400 }}>/ {data.goal.toLocaleString()}</span>
        </span>
      </div>

      {/* Progress bar */}
      <div
        className="w-full rounded-full overflow-hidden"
        style={{ height: `${Math.max(8, Math.round(fontSize * 0.6))}px`, background: "rgba(255,255,255,0.2)" }}
      >
        <div
          className="h-full rounded-full transition-all duration-700 ease-out"
          style={{ width: `${progress}%`, background: color, boxShadow: `0 0 8px ${color}88` }}
        />
      </div>

      {/* Percentage */}
      <div
        className="mt-1 text-right"
        style={{ fontSize: `${Math.round(fontSize * 0.75)}px`, color: "rgba(255,255,255,0.7)", textShadow: "0 1px 3px rgba(0,0,0,0.6)" }}
      >
        {progress.toFixed(1)}%
      </div>
    </div>
  )
}
