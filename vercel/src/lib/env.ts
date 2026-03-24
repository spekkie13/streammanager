function requireEnv(name: string): string {
  const value = process.env[name]
  if (!value) throw new Error(`Missing required environment variable: ${name}`)
  return value
}

export const env = {
  twitchClientId: requireEnv("TWITCH_CLIENT_ID"),
  twitchClientSecret: requireEnv("TWITCH_CLIENT_SECRET"),
  databaseUrl: requireEnv("DATABASE_URL"),
  twitchWebhookSecret: requireEnv("TWITCH_WEBHOOK_SECRET"),
  googleClientId: requireEnv("GOOGLE_CLIENT_ID"),
  googleClientSecret: requireEnv("GOOGLE_CLIENT_SECRET"),
  cronSecret: requireEnv("CRON_SECRET"),
}
