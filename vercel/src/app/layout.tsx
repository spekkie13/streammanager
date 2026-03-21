import type { Metadata } from "next"
import { Inter } from "next/font/google"
import "./globals.css"
import { SessionProvider } from "./session-provider"
import { getServerSession } from "next-auth"
import { authOptions } from "@/lib/auth"

const inter = Inter({ subsets: ["latin"] })

export const metadata: Metadata = {
  title: "CreatorDeck",
  description: "Your creator command centre — streams, videos, and everything in between.",
}

export default async function RootLayout({ children }: { children: React.ReactNode }) {
  const session = await getServerSession(authOptions)
  return (
    <html lang="en">
      <body className={inter.className}>
        <SessionProvider session={session}>
          {children}
        </SessionProvider>
      </body>
    </html>
  )
}
