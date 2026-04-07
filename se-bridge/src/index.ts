import express from 'express'
import { env } from './env'
import { ChannelRegistry } from './registry'
import { creatorDeckClient } from './creatordeck-client'
import { buildManagementRouter } from './management-api'

async function main(): Promise<void> {
  const registry = new ChannelRegistry()

  // Bootstrap: restore all active SE connections from CreatorDeck
  console.log('[bridge] fetching active SE accounts from CreatorDeck...')
  try {
    const accounts = await creatorDeckClient.fetchActiveAccounts()
    console.log(`[bridge] bootstrapping ${accounts.length} channel(s)`)
    for (const account of accounts) {
      registry.register(account.channelId, account.accessToken, account.refreshToken)
    }
  } catch (err) {
    console.error('[bridge] bootstrap failed — starting with no connections:', err)
  }

  // Start management API
  const app = express()
  app.use(express.json())
  app.use('/', buildManagementRouter(registry))

  app.listen(env.port, () => {
    console.log(`[bridge] management API listening on port ${env.port}`)
  })
}

main().catch(err => {
  console.error('[bridge] fatal startup error:', err)
  process.exit(1)
})
