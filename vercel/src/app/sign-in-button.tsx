"use client"
import { signIn } from "next-auth/react"

type Props = {
  variant?: "primary" | "ghost"
}

export function SignInButton({ variant = "primary" }: Props) {
  if (variant === "ghost") {
    return (
      <button
        onClick={() => signIn("twitch", { callbackUrl: "/dashboard" })}
        className="text-sm text-zinc-600 dark:text-zinc-400 hover:text-zinc-900 dark:hover:text-white transition-colors"
      >
        Sign in
      </button>
    )
  }

  return (
    <button
      onClick={() => signIn("twitch", { callbackUrl: "/dashboard" })}
      className="inline-flex items-center gap-3 bg-purple-500 hover:bg-purple-600 text-white font-semibold px-8 py-3 rounded-xl transition-colors text-base"
    >
      <svg className="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
        <path d="M11.571 4.714h1.715v5.143H11.57zm4.715 0H18v5.143h-1.714zM6 0L1.714 4.286v15.428h5.143V24l4.286-4.286h3.428L22.286 12V0zm14.571 11.143l-3.428 3.428h-3.429l-3 3v-3H6.857V1.714h13.714z" />
      </svg>
      Sign in with Twitch
    </button>
  )
}
