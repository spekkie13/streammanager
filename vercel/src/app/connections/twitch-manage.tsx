"use client"
import { useState } from "react"

type Props = {
  webhookUrl: string
  subscriptionsRegistered: boolean
}

export function TwitchManage({ webhookUrl, subscriptionsRegistered }: Props) {
  const [registering, setRegistering] = useState(false)
  const [status, setStatus] = useState<"idle" | "success" | "error">("idle")
  const [copied, setCopied] = useState(false)

  async function register() {
    setRegistering(true)
    setStatus("idle")
    const res = await fetch("/api/register-subscriptions", { method: "POST" })
    setStatus(res.ok ? "success" : "error")
    setRegistering(false)
  }

  function copy() {
    navigator.clipboard.writeText(webhookUrl)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <div className="px-6 py-4 space-y-4 border-t border-zinc-200 dark:border-zinc-800">
      {/* Webhook URL */}
      <div className="space-y-1.5">
        <label className="text-xs text-zinc-500">Webhook URL</label>
        <div className="flex items-center gap-2">
          <code className="flex-1 bg-zinc-100 dark:bg-zinc-800 text-xs text-zinc-700 dark:text-zinc-300 px-3 py-2 rounded-lg truncate">
            {webhookUrl}
          </code>
          <button
            onClick={copy}
            className="shrink-0 text-xs text-zinc-500 hover:text-zinc-900 dark:hover:text-white transition-colors px-2 py-2"
          >
            {copied ? "✓" : "Copy"}
          </button>
        </div>
      </div>

      {/* Register subscriptions */}
      <div className="flex items-center gap-3">
        <button
          onClick={register}
          disabled={registering}
          className="text-xs bg-zinc-100 dark:bg-zinc-800 hover:bg-zinc-200 dark:hover:bg-zinc-700 border border-zinc-300 dark:border-zinc-700 text-zinc-900 dark:text-white px-3 py-1.5 rounded-lg transition-colors disabled:opacity-50"
        >
          {registering ? "Registering..." : subscriptionsRegistered ? "Re-register subscriptions" : "Register subscriptions"}
        </button>
        {status === "success" && <span className="text-xs text-green-500">Subscriptions registered.</span>}
        {status === "error" && <span className="text-xs text-red-400">Failed — check your Twitch app scopes.</span>}
      </div>
    </div>
  )
}
