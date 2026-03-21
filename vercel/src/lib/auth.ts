import { NextAuthOptions } from "next-auth"
import TwitchProvider from "next-auth/providers/twitch"
import { db } from "./db"
import { users } from "./schema"
import { eq } from "drizzle-orm"
import { randomBytes } from "crypto"

async function upsertUser(twitchId: string, login: string, displayName: string, accessToken: string, refreshToken: string) {
  const existing = await db.select().from(users).where(eq(users.twitchId, twitchId)).limit(1)

  if (existing.length === 0) {
    const apiKey = randomBytes(32).toString("hex")
    await db.insert(users).values({
      twitchId,
      twitchLogin: login,
      twitchDisplayName: displayName,
      accessToken,
      refreshToken,
      apiKey,
    })
    return apiKey
  } else {
    await db.update(users)
      .set({ twitchLogin: login, twitchDisplayName: displayName, accessToken, refreshToken })
      .where(eq(users.twitchId, twitchId))
    return existing[0].apiKey
  }
}

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
      await upsertUser(
        p.sub,
        p.preferred_username,
        p.preferred_username,
        account.access_token ?? "",
        account.refresh_token ?? ""
      )
      return true
    },
    async jwt({ token, account, profile }) {
      if (account && profile) {
        const p = profile as Record<string, string>
        token.twitchId = p.sub
        token.displayName = p.preferred_username
        const row = await db.select({ apiKey: users.apiKey }).from(users).where(eq(users.twitchId, p.sub)).limit(1)
        token.apiKey = row[0]?.apiKey ?? ""
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
