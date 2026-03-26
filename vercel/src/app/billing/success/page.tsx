"use client"
import { useEffect } from "react"
import { useSession } from "next-auth/react"
import { useRouter } from "next/navigation"

export default function BillingSuccessPage() {
  const { update } = useSession()
  const router = useRouter()

  useEffect(() => {
    update().then(() => {
      router.replace("/billing")
    })
  }, [update, router])

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950 flex items-center justify-center">
      <div className="text-center space-y-3">
        <p className="text-2xl">🎉</p>
        <p className="text-lg font-semibold">Payment successful!</p>
        <p className="text-sm text-zinc-500 dark:text-zinc-400">Activating your plan...</p>
      </div>
    </div>
  )
}