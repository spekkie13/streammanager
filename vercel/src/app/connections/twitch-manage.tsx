"use client"
import { useState } from "react"

type Props = {
  webhookUrl: string
  subscriptionsRegistered: boolean
}

export function TwitchManage({ webhookUrl, subscriptionsRegistered: initiallyRegistered }: Props) {
  const [registering, setRegistering] = useState(false)
  const [status, setStatus] = useState<"idle" | "success" | "error">("idle")
  const [copied, setCopied] = useState(false)
  const [registered, setRegistered] = useState(initiallyRegistered)

  async function register() {
    setRegistering(true)
    setStatus("idle")
    const res = await fetch("/api/register-subscriptions", { method: "POST" })
    if (res.ok) {
      setStatus("success")
      setRegistered(true)
    } else {
      setStatus("error")
    }
    setRegistering(false)
  }

  function copy() {
    navigator.clipboard.writeText(webhookUrl)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <div className="border-t border-zinc-200 dark:border-zinc-800">
      {/* Setup prompt — prominent when not yet registered */}
      {!registered && (
        <div className="mx-6 my-4 bg-amber-50 dark:bg-amber-950/20 border border-amber-200 dark:border-amber-800/40 rounded-lg p-4 space-y-3">
          <div>
            <p className="text-sm font-medium text-amber-800 dark:text-amber-300">Action required</p>
            <p className="text-xs text-amber-700 dark:text-amber-500 mt-0.5">
              Register your EventSub subscriptions so Twitch can deliver follows, subs, bits and raids to CreatorDeck.
              This only needs to be done once.
            </p>
          </div>
          <button
            onClick={register}
            disabled={registering}
            className="bg-amber-500 hover:bg-amber-600 disabled:opacity-50 text-white text-sm font-medium px-4 py-2 rounded-lg transition-colors"
          >
            {registering ? "Registering..." : "Register subscriptions"}
          </button>
          {status === "error" && <p className="text-xs text-red-400">Failed — check your Twitch app scopes.</p>}
        </div>
      )}

      {/* Expanded details — always shown */}
      <div className="px-6 py-4 space-y-4">
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

        {/* Re-register when already set up */}
        {registered && (
          <div className="flex items-center gap-3">
            <span className="flex items-center gap-1.5 text-xs text-green-500">
              <span className="w-1.5 h-1.5 rounded-full bg-green-500 inline-block" />
              Subscriptions registered
            </span>
            <button
              onClick={register}
              disabled={registering}
              className="text-xs text-zinc-500 hover:text-zinc-900 dark:hover:text-white border border-zinc-200 dark:border-zinc-700 px-3 py-1.5 rounded-lg transition-colors disabled:opacity-50"
            >
              {registering ? "Registering..." : "Re-register"}
            </button>
            {status === "success" && <span className="text-xs text-green-500">Done.</span>}
            {status === "error" && <span className="text-xs text-red-400">Failed — check your Twitch app scopes.</span>}
          </div>
        )}
      </div>
    </div>
  )
}
