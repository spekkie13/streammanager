import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { redirect } from "next/navigation"
import { SignInButton } from "./sign-in-button"
import { WaitlistForm } from "./waitlist-form"

export default async function LandingPage() {
  const session = await getServerSession(authOptions)
  if (session) redirect("/dashboard")

  return (
    <main className="min-h-screen flex items-center justify-center">
      <div className="text-center space-y-8 px-4 max-w-lg w-full">
        <div className="space-y-3">
          <h1 className="text-5xl font-bold tracking-tight">
            Stream<span className="text-purple-500">Stats</span>
          </h1>
          <p className="text-zinc-400 text-lg max-w-md mx-auto">
            Track your Twitch subscriber goal in real time — including subs that happen while you&apos;re offline.
          </p>
        </div>

        <div className="border-t border-zinc-800 pt-8 space-y-4">
          <p className="text-zinc-300 text-sm font-medium">More features coming soon — get notified</p>
          <WaitlistForm />
        </div>

        <div className="border-t border-zinc-800 pt-6 space-y-3">
          <p className="text-zinc-500 text-xs">Already have access?</p>
          <SignInButton />
        </div>
      </div>
    </main>
  )
}
