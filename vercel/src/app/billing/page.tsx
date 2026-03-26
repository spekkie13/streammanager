import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { redirect } from "next/navigation"
import { AppHeader } from "@/components/app-header"
import { TIER_LABELS, TIER_PRICES, type SubscriptionTier } from "@/lib/gates"

const TIERS: SubscriptionTier[] = ["free", "tier1", "tier2", "tier3"]

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

function TierCard({
  tier,
  currentTier,
}: {
  tier: SubscriptionTier
  currentTier: SubscriptionTier
}) {
  const isCurrent = tier === currentTier
  const isPopular = tier === "tier1"

  return (
    <div className={`relative flex flex-col rounded-2xl border p-6 gap-5 ${
      isCurrent
        ? "border-purple-500 ring-1 ring-purple-500 bg-white dark:bg-zinc-900"
        : "border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900"
    }`}>
      {isPopular && !isCurrent && (
        <div className="absolute -top-3 left-1/2 -translate-x-1/2">
          <span className="bg-purple-600 text-white text-xs font-semibold px-3 py-1 rounded-full">
            Most popular
          </span>
        </div>
      )}
      {isCurrent && (
        <div className="absolute -top-3 left-1/2 -translate-x-1/2">
          <span className="bg-zinc-800 dark:bg-zinc-200 text-white dark:text-zinc-900 text-xs font-semibold px-3 py-1 rounded-full">
            Current plan
          </span>
        </div>
      )}

      <div>
        <p className="text-sm font-semibold text-zinc-500 dark:text-zinc-400">{TIER_LABELS[tier]}</p>
        <p className="text-3xl font-bold mt-1">{TIER_PRICES[tier]}</p>
      </div>

      <ul className="flex-1 space-y-2">
        {TIER_FEATURES[tier].map(f => (
          <li key={f} className="flex items-start gap-2 text-sm text-zinc-700 dark:text-zinc-300">
            <span className="text-purple-500 mt-0.5 shrink-0">✓</span>
            {f}
          </li>
        ))}
      </ul>

      <button
        disabled
        className={`w-full py-2.5 rounded-lg text-sm font-medium transition-colors ${
          isCurrent
            ? "bg-zinc-100 dark:bg-zinc-800 text-zinc-400 dark:text-zinc-500 cursor-default"
            : tier === "free"
            ? "bg-zinc-100 dark:bg-zinc-800 text-zinc-400 dark:text-zinc-500 cursor-default"
            : "bg-purple-600 text-white opacity-50 cursor-not-allowed"
        }`}
      >
        {isCurrent ? "Current plan" : tier === "free" ? "Free forever" : "Coming soon"}
      </button>
    </div>
  )
}

export default async function BillingPage() {
  const session = await getServerSession(authOptions)
  if (!session) redirect("/")

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <AppHeader displayName={session.displayName} />

      <main className="max-w-5xl mx-auto px-6 py-10 space-y-8">

        <div className="space-y-1">
          <h1 className="text-xl font-semibold tracking-tight">Billing & Plans</h1>
          <p className="text-sm text-zinc-500 dark:text-zinc-400">
            You are currently on the <span className="font-medium text-zinc-700 dark:text-zinc-300">{TIER_LABELS[session.tier]}</span> plan.
            Paid plans are coming soon.
          </p>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {TIERS.map(tier => (
            <TierCard key={tier} tier={tier} currentTier={session.tier} />
          ))}
        </div>

        <p className="text-xs text-center text-zinc-400 dark:text-zinc-600">
          Prices shown in USD. Billing will be handled via Stripe. Cancel anytime.
        </p>

      </main>
    </div>
  )
}
