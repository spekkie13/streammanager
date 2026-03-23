import { NextAuthOptions } from "next-auth"
import TwitchProvider from "next-auth/providers/twitch"
import GoogleProvider from "next-auth/providers/google"
import { linkedAccountsRepository } from "@/repositories"
import { env } from "@/lib/env"

async function fetchYouTubeChannelId(accessToken: string): Promise<string | null> {
  const res = await fetch("https://www.googleapis.com/youtube/v3/channels?part=id&mine=true", {
    headers: { Authorization: `Bearer ${accessToken}` },
  })
  const data = await res.json()
  return data.items?.[0]?.id ?? null
}

export const authOptions: NextAuthOptions = {
  providers: [
    TwitchProvider({
      clientId: env.twitchClientId,
      clientSecret: env.twitchClientSecret,
      authorization: {
        params: {
          scope: "openid user:read:email channel:read:subscriptions moderator:read:followers",
        },
      },
    }),
    GoogleProvider({
      clientId: env.googleClientId,
      clientSecret: env.googleClientSecret,
      authorization: {
        params: {
          scope: "openid email profile https://www.googleapis.com/auth/youtube.readonly",
          access_type: "offline",
          prompt: "consent",
        },
      },
    }),
  ],
  session: { strategy: "jwt" },
  callbacks: {
    async signIn({ account }) {
      return account?.provider === "twitch" || account?.provider === "google"
    },
    async jwt({ token, account, profile }) {
      if (account && profile) {
        const p = profile as Record<string, string>
        const existingUserId = token.userId as string | undefined

        if (account.provider === "twitch") {
          if (existingUserId) {
            // Linking Twitch to an existing session (e.g. YouTube user connecting Twitch)
            await linkedAccountsRepository.upsertForUser(existingUserId, {
              provider: "twitch",
              providerAccountId: p.sub,
              login: p.preferred_username,
              displayName: p.preferred_username,
              accessToken: account.access_token ?? "",
              refreshToken: account.refresh_token ?? "",
            })
            token.twitchId = p.sub
          } else {
            const { userId, apiKey } = await linkedAccountsRepository.upsertWithUser({
              provider: "twitch",
              providerAccountId: p.sub,
              login: p.preferred_username,
              displayName: p.preferred_username,
              accessToken: account.access_token ?? "",
              refreshToken: account.refresh_token ?? "",
            })
            token.userId = userId
            token.twitchId = p.sub
            token.youtubeChannelId = null
            token.displayName = p.preferred_username
            token.apiKey = apiKey
          }

        } else if (account.provider === "google") {
          const channelId = await fetchYouTubeChannelId(account.access_token ?? "")
          if (!channelId) return token

          if (existingUserId) {
            // Linking YouTube to an existing session (e.g. Twitch user connecting YouTube)
            await linkedAccountsRepository.upsertForUser(existingUserId, {
              provider: "youtube",
              providerAccountId: channelId,
              login: channelId,
              displayName: p.name ?? channelId,
              accessToken: account.access_token ?? "",
              refreshToken: account.refresh_token ?? "",
            })
            token.youtubeChannelId = channelId
          } else {
            const { userId, apiKey } = await linkedAccountsRepository.upsertWithUser({
              provider: "youtube",
              providerAccountId: channelId,
              login: channelId,
              displayName: p.name ?? channelId,
              accessToken: account.access_token ?? "",
              refreshToken: account.refresh_token ?? "",
            })
            token.userId = userId
            token.twitchId = null
            token.youtubeChannelId = channelId
            token.displayName = p.name ?? channelId
            token.apiKey = apiKey
          }
        }
      }
      return token
    },
    async session({ session, token }) {
      session.userId = token.userId as string
      session.twitchId = token.twitchId as string | null
      session.youtubeChannelId = token.youtubeChannelId as string | null
      session.displayName = token.displayName as string
      session.apiKey = token.apiKey as string
      return session
    },
  },
  pages: { signIn: "/" },
}
