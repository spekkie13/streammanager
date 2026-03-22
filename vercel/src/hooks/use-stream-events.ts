"use client"

import { useState, useEffect } from "react"
import type { LiveEvent } from "@/types/events"

const MAX_EVENTS = 50

export function useStreamEvents(initial: LiveEvent[] = []): LiveEvent[] {
  const [events, setEvents] = useState<LiveEvent[]>(initial)

  useEffect(() => {
    const es = new EventSource("/api/events/stream")

    es.onmessage = (e: MessageEvent) => {
      const incoming: LiveEvent[] = JSON.parse(e.data)
      setEvents(prev => [...incoming, ...prev].slice(0, MAX_EVENTS))
    }

    es.onerror = () => {
      es.close()
    }

    return () => es.close()
  }, [])

  return events
}
