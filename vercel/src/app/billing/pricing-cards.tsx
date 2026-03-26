"use client"
import { useState } from "react"
import { useRouter } from "next/navigation"
import { TIER_LABELS, TIER_MONTHLY_PRICES, TIER_ANNUAL_PRICES } from "@/lib/gates"
import type { SubscriptionTier } from "@/lib/gates"

type BillingCycle = "monthly" | "annual"
type PaidTier = "tier1" | "tier2" | "tier3"

const PAID_TIERS: PaidTier[] = ["tier1", "tier2", "tier3"]

const TIER_FEATURES: Record<SubscriptionTier, string[]> = {
  free: [
    "Unified live dashboard (Twitch + YouTube)",
    "Real-time event feed",
    "Basic OBS overlays",
    "Goals tracking",
    "7-day event history",
  ],
  tier1: [
    "Everything in Free",
    "Full analytics history (30d / 90d)",
    "Per-session breakdowns",
    "Platform comparison charts",
  ],
  tier2: [
    "Everything in Tier 1",
    "Custom alert overlays for OBS",
    "Stream info management (title, game)",
    "Cross-platform goals",
    "Simultaneous go-live on Twitch + YouTube",
  ],
  tier3: [
    "Everything in Tier 2",
    "AI stream analysis",
    "VOD transcription insights",
    "Weekly improvement reports",
    "Retention coaching",
  ],
}

type Props = {
  currentTier: SubscriptionTier
  hasSubscription: boolean
  prices: {
    tier1: { monthly: string; annual: string }
    tier2: { monthly: string; annual: string }
    tier3: { monthly: string; annual: string }
  }
}

export function PricingCards({ currentTier, hasSubscription, prices }: Props) {
  const router = useRouter()
  const [cycle, setCycle] = useState<BillingCycle>("monthly")
  const [loading, setLoading] = useState<string | null>(null)

  async function handleUpgrade(tier: PaidTier) {
    const priceId = prices[tier][cycle]
    setLoading(tier)
    try {
      const res = await fetch("/api/stripe/checkout", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ priceId }),
      })
      const { url } = await res.json()
      if (url) window.location.href = url
    } finally {
      setLoading(null)
    }
  }

  async function handleManage() {
    setLoading("portal")
    try {
      const res = await fetch("/api/stripe/portal", { method: "POST" })
      const { url } = await res.json()
      if (url) window.location.href = url
    } finally {
      setLoading(null)
    }
  }

  const displayPrice = (tier: SubscriptionTier) =>
    cycle === "monthly" ? TIER_MONTHLY_PRICES[tier] : TIER_ANNUAL_PRICES[tier]

  return (
    <div className="space-y-6">
      {/* Billing cycle toggle */}
      <div className="flex items-center justify-center gap-1 bg-zinc-100 dark:bg-zinc-800/60 rounded-lg p-1 w-fit mx-auto">
        {(["monthly", "annual"] as BillingCycle[]).map(c => (
          <button
            key={c}
            onClick={() => setCycle(c)}
            className={`text-sm px-4 py-1.5 rounded-md font-medium transition-colors capitalize ${
              cycle === c
                ? "bg-white dark:bg-zinc-700 text-zinc-900 dark:text-white shadow-sm"
                : "text-zinc-500 hover:text-zinc-700 dark:hover:text-zinc-300"
            }`}
          >
            {c}
            {c === "annual" && (
              <span className="ml-1.5 text-xs text-green-500 font-semibold">Save ~15%</span>
            )}
          </button>
        ))}
      </div>

      {/* Plan cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">

        {/* Free */}
        <div className={`relative flex flex-col rounded-2xl border p-6 gap-5 ${
          currentTier === "free"
            ? "border-purple-500 ring-1 ring-purple-500 bg-white dark:bg-zinc-900"
            : "border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900"
        }`}>
          {currentTier === "free" && <CurrentPlanBadge />}
          <div>
            <p className="text-sm font-semibold text-zinc-500 dark:text-zinc-400">{TIER_LABELS.free}</p>
            <p className="text-3xl font-bold mt-1">Free</p>
          </div>
          <ul className="flex-1 space-y-2">
            {TIER_FEATURES.free.map(f => <Feature key={f} text={f} />)}
          </ul>
          <button disabled className="w-full py-2.5 rounded-lg text-sm font-medium bg-zinc-100 dark:bg-zinc-800 text-zinc-400 cursor-default">
            {currentTier === "free" ? "Current plan" : "Free forever"}
          </button>
        </div>

        {/* Paid tiers */}
        {PAID_TIERS.map(tier => {
          const isCurrent = currentTier === tier
          const isPopular = tier === "tier1"
          const isLoadingThis = loading === tier

          return (
            <div
              key={tier}
              className={`relative flex flex-col rounded-2xl border p-6 gap-5 ${
                isCurrent
                  ? "border-purple-500 ring-1 ring-purple-500 bg-white dark:bg-zinc-900"
                  : isPopular
                  ? "border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-900"
                  : "border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900"
              }`}
            >
              {isCurrent && <CurrentPlanBadge />}
              {isPopular && !isCurrent && (
                <div className="absolute -top-3 left-1/2 -translate-x-1/2">
                  <span className="bg-purple-600 text-white text-xs font-semibold px-3 py-1 rounded-full">Most popular</span>
                </div>
              )}

              <div>
                <p className="text-sm font-semibold text-zinc-500 dark:text-zinc-400">{TIER_LABELS[tier]}</p>
                <p className="text-3xl font-bold mt-1">{displayPrice(tier)}</p>
              </div>

              <ul className="flex-1 space-y-2">
                {TIER_FEATURES[tier].map(f => <Feature key={f} text={f} />)}
              </ul>

              {isCurrent ? (
                <button
                  onClick={handleManage}
                  disabled={loading !== null}
                  className="w-full py-2.5 rounded-lg text-sm font-medium border border-zinc-300 dark:border-zinc-600 text-zinc-700 dark:text-zinc-300 hover:bg-zinc-50 dark:hover:bg-zinc-800 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {loading === "portal" ? <Spinner /> : "Manage subscription"}
                </button>
              ) : (
                <button
                  onClick={() => handleUpgrade(tier)}
                  disabled={loading !== null}
                  className="w-full py-2.5 rounded-lg text-sm font-medium bg-purple-600 hover:bg-purple-500 text-white transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {isLoadingThis ? <Spinner /> : `Upgrade to ${TIER_LABELS[tier]}`}
                </button>
              )}
            </div>
          )
        })}
      </div>

      {hasSubscription && (
        <p className="text-xs text-center text-zinc-400 dark:text-zinc-600">
          To cancel or change your plan, use{" "}
          <button onClick={handleManage} className="underline hover:text-zinc-600 dark:hover:text-zinc-400 transition-colors">
            Manage subscription
          </button>
          . Your access continues until the end of the billing period.
        </p>
      )}
    </div>
  )
}

function CurrentPlanBadge() {
  return (
    <div className="absolute -top-3 left-1/2 -translate-x-1/2">
      <span className="bg-zinc-800 dark:bg-zinc-200 text-white dark:text-zinc-900 text-xs font-semibold px-3 py-1 rounded-full">
        Current plan
      </span>
    </div>
  )
}

function Feature({ text }: { text: string }) {
  return (
    <li className="flex items-start gap-2 text-sm text-zinc-700 dark:text-zinc-300">
      <span className="text-purple-500 mt-0.5 shrink-0">✓</span>
      {text}
    </li>
  )
}

function Spinner() {
  return (
    <span className="flex items-center justify-center">
      <span className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
    </span>
  )
}