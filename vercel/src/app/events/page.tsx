import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"
import { redirect } from "next/navigation"
import { EventsClient } from "./events-client"

export default async function EventsPage() {
  const session = await getServerSession(authOptions)
  if (!session) redirect("/")

  return <EventsClient displayName={session.displayName} />
}
