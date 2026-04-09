import { ChannelRegistry } from './registry'
import { creatorDeckClient } from './creatordeck-client'

const SYNC_INTERVAL_MS = 5 * 60 * 1000

async function syncAccounts(registry: ChannelRegistry): Promise<void> {
  const accounts = await creatorDeckClient.fetchActiveAccounts()
  const fetched = new Map(accounts.map(a => [a.channelId, a.jwtToken]))

  // Start new connections
  for (const [channelId, jwtToken] of fetched) {
    registry.register(channelId, jwtToken)
  }

  // Stop connections for accounts that have been removed
  for (const channelId of registry.activeChannelIds()) {
    if (!fetched.has(channelId)) {
      registry.deregister(channelId)
    }
  }
}

async function main(): Promise<void> {
  const registry = new ChannelRegistry()

  console.log('[bridge] fetching active SE accounts from CreatorDeck...')
  try {
    await syncAccounts(registry)
    console.log('[bridge] initial sync complete')
  } catch (err) {
    console.error('[bridge] initial sync failed — starting with no connections:', err)
  }

  setInterval(async () => {
    console.log('[bridge] syncing accounts...')
    try {
      await syncAccounts(registry)
    } catch (err) {
      console.error('[bridge] sync failed:', err)
    }
  }, SYNC_INTERVAL_MS)
}

main().catch(err => {
  console.error('[bridge] fatal startup error:', err)
  process.exit(1)
})