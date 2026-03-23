import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { redirect } from "next/navigation"
import { WaitlistForm } from "./waitlist-form"
import { SignInButton } from "./sign-in-button"

const FEATURES = [
  { icon: "⚡", text: "Live event feed — subs, follows, bits and raids in real time" },
  { icon: "📊", text: "Event history — filter and sort every support event from your streams" },
  { icon: "🎯", text: "Sub goal tracker — set a target and watch it fill up live" },
  { icon: "🎬", text: "YouTube support — Super Chats and memberships, coming soon" },
]

export default async function LandingPage() {
  const session = await getServerSession(authOptions)
  if (session) redirect("/dashboard")

  return (
    <main className="min-h-screen flex items-center justify-center py-16">
      <div className="text-center space-y-10 px-4 max-w-lg w-full">
        <div className="space-y-3">
          <h1 className="text-5xl font-bold tracking-tight text-zinc-900 dark:text-white">
            Creator<span className="text-purple-500">Deck</span>
          </h1>
          <p className="text-zinc-500 text-lg max-w-md mx-auto">
            Your creator command centre — streams, videos, and everything in between.
          </p>
        </div>

        <ul className="text-left space-y-3">
          {FEATURES.map(f => (
            <li key={f.text} className="flex items-start gap-3 text-sm text-zinc-600 dark:text-zinc-400">
              <span className="text-base leading-snug">{f.icon}</span>
              <span>{f.text}</span>
            </li>
          ))}
        </ul>

        <div className="border-t border-zinc-200 dark:border-zinc-800 pt-8 space-y-4">
          <p className="text-zinc-600 dark:text-zinc-300 text-sm font-medium">More features coming soon — get notified</p>
          <WaitlistForm />
        </div>

        <div className="border-t border-zinc-200 dark:border-zinc-800 pt-6 space-y-3">
          <p className="text-zinc-500 text-xs">Already have access?</p>
          <SignInButton />
        </div>
      </div>
    </main>
  )
}
