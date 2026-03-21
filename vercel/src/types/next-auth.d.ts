import "next-auth"

declare module "next-auth" {
  interface Session {
    twitchId: string
    displayName: string
    apiKey: string
  }
}

declare module "next-auth/jwt" {
  interface JWT {
    twitchId: string
    displayName: string
    apiKey: string
  }
}
