import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { redirect } from "next/navigation"
import { userRepository } from "@/repositories"
import { SetupWizard } from "./setup-wizard"

export default async function SetupPage() {
  const session = await getServerSession(authOptions)
  if (!session) redirect("/")

  const user = await userRepository.findByTwitchId(session.twitchId)
  if (user?.onboardingCompleted) redirect("/dashboard")

  return (
    <div className="min-h-screen bg-white dark:bg-zinc-950 flex items-center justify-center p-6">
      <SetupWizard displayName={session.displayName} />
    </div>
  )
}
