"use client"
import { useEffect } from "react"
import { useSession } from "next-auth/react"
import { useSearchParams, useRouter } from "next/navigation"

export function ConnectionsUpdater() {
  const { update } = useSession()
  const searchParams = useSearchParams()
  const router = useRouter()

  useEffect(() => {
    if (searchParams.get("linked")) {
      // Trigger a session refresh so the JWT picks up the newly linked account from DB
      update().then(() => router.replace("/connections"))
    }
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

  return null
}
