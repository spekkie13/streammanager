import type { Metadata } from "next"
import { Inter } from "next/font/google"
import "../globals.css"

const inter = Inter({ subsets: ["latin"] })

export const metadata: Metadata = {
  title: "CreatorDeck Widget",
}

export default function WidgetLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body className={`${inter.className} bg-transparent`} style={{ background: "transparent" }}>
        {children}
      </body>
    </html>
  )
}
