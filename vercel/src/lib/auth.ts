import { NextAuthOptions } from "next-auth"
import TwitchProvider from "next-auth/providers/twitch"
import { userRepository } from "@/repositories/user.repository"

export const authOptions: NextAuthOptions = {
  providers: [
    TwitchProvider({
      clientId: process.env.TWITCH_CLIENT_ID!,
      clientSecret: process.env.TWITCH_CLIENT_SECRET!,
      authorization: {
        params: {
          scope: "openid user:read:email channel:read:subscriptions moderator:read:followers",
        },
      },
    }),
  ],
  session: { strategy: "jwt" },
  callbacks: {
    async signIn({ account, profile }) {
      if (!account || !profile) return false
      const p = profile as Record<string, string>
      await userRepository.upsert(p.sub, p.preferred_username, p.preferred_username, account.access_token ?? "", account.refresh_token ?? "")
      return true
    },
    async jwt({ token, account, profile }) {
      if (account && profile) {
        const p = profile as Record<string, string>
        token.twitchId = p.sub
        token.displayName = p.preferred_username
        const user = await userRepository.findByTwitchId(p.sub)
        token.apiKey = user?.apiKey ?? ""
      }
      return token
    },
    async session({ session, token }) {
      session.twitchId = token.twitchId as string
      session.displayName = token.displayName as string
      session.apiKey = token.apiKey as string
      return session
    },
  },
  pages: { signIn: "/" },
}
