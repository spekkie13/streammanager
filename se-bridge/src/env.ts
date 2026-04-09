function requireEnv(name: string): string {
  const value = process.env[name]
  if (!value) throw new Error(`Missing required environment variable: ${name}`)
  return value
}

export const env = {
  creatorDeckUrl: requireEnv('CREATORDECK_URL'),
  bridgeSecret: requireEnv('BRIDGE_SECRET'),
  port: parseInt(process.env['PORT'] ?? '3001', 10),
}