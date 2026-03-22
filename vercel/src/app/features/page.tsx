import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { redirect } from "next/navigation"
import { AppHeader } from "@/components/app-header"

type FeatureRowProps = {
  name: string
  description: string
  comingSoon?: boolean
}

function FeatureRow({ name, description, comingSoon }: FeatureRowProps) {
  return (
    <div className="px-6 py-5 flex items-center justify-between gap-6">
      <div className="space-y-0.5">
        <div className="flex items-center gap-2">
          <span className="text-sm font-medium text-white">{name}</span>
          {comingSoon && (
            <span className="text-xs text-zinc-500 bg-zinc-800 px-2 py-0.5 rounded">Coming soon</span>
          )}
        </div>
        <p className="text-xs text-zinc-500">{description}</p>
      </div>
      <div
        className={`relative shrink-0 w-10 h-6 rounded-full transition-colors ${comingSoon ? "bg-zinc-800 cursor-not-allowed opacity-40" : "bg-purple-500 cursor-pointer"}`}
      >
        <span className={`absolute top-1 w-4 h-4 bg-white rounded-full shadow transition-all ${comingSoon ? "left-1" : "left-5"}`} />
      </div>
    </div>
  )
}

export default async function FeaturesPage() {
  const session = await getServerSession(authOptions)
  if (!session) redirect("/")

  return (
    <div className="min-h-screen bg-[#0a0a0a] text-white">
      <AppHeader displayName={session.displayName} />
      <main className="max-w-3xl mx-auto px-6 py-10 space-y-6">
        <div>
          <h1 className="text-2xl font-bold">Features</h1>
          <p className="text-zinc-500 text-sm mt-1">Enable or disable individual features for your account.</p>
        </div>

        <div className="bg-zinc-900 border border-zinc-800 rounded-xl divide-y divide-zinc-800">
          <FeatureRow
            name="Event tracking"
            description="Record follows, subs, bits and raids to your event history."
          />
          <FeatureRow
            name="Live event feed"
            description="Stream live events to your dashboard in real time."
          />
          <FeatureRow
            name="OBS overlay"
            description="Browser source alerts for subs, follows and raids."
            comingSoon={true}
          />
          <FeatureRow
            name="Spotify widget"
            description="Show your currently playing track in the dashboard."
            comingSoon={true}
          />
          <FeatureRow
            name="Analytics"
            description="Charts and trends across your stream history."
            comingSoon={true}
          />
        </div>
      </main>
    </div>
  )
}
