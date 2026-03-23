import "next-auth"

declare module "next-auth" {
  interface Session {
    userId: string
    twitchId: string | null
    youtubeChannelId: string | null
    displayName: string
    apiKey: string
  }
}

declare module "next-auth/jwt" {
  interface JWT {
    userId: string
    twitchId: string | null
    youtubeChannelId: string | null
    displayName: string
    apiKey: string
  }
}
