import { env } from './env'

export interface SeAccount {
  channelId: string
  jwtToken: string
}

class CreatorDeckClient {
  private readonly headers = {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${env.bridgeSecret}`,
  }

  async fetchActiveAccounts(): Promise<SeAccount[]> {
    const res = await fetch(`${env.creatorDeckUrl}/api/internal/se-accounts`, {
      headers: this.headers,
    })
    if (!res.ok) throw new Error(`[creatordeck] fetchActiveAccounts failed (${res.status})`)
    const data = await res.json() as { accounts: SeAccount[] }
    return data.accounts
  }

  async forwardMessage(
    channelId: string,
    eventId: string,
    userDisplayName: string | null,
    userId: string | null,
    message: string,
    occurredAt: string,
  ): Promise<void> {
    const res = await fetch(`${env.creatorDeckUrl}/api/webhooks/streamelements`, {
      method: 'POST',
      headers: this.headers,
      body: JSON.stringify({ channelId, eventId, userDisplayName, userId, message, occurredAt }),
    })
    if (!res.ok) {
      const body = await res.text().catch(() => '(unreadable)')
      throw new Error(`[creatordeck] forwardMessage failed (${res.status}): ${body}`)
    }
  }
}

export const creatorDeckClient = new CreatorDeckClient()