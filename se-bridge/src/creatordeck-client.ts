import { env } from './env'

export interface SeAccount {
  channelId: string
  accessToken: string
  refreshToken: string
}

export interface IncomingChatMessage {
  id: string
  userDisplayName: string
  userId: string | null
  message: string
  occurredAt: string
}

class CreatorDeckClient {
  private readonly headers = {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${env.northflankWebhookSecret}`,
  }

  async fetchActiveAccounts(): Promise<SeAccount[]> {
    const res = await fetch(`${env.creatorDeckUrl}/api/internal/se-accounts`, {
      headers: this.headers,
    })
    if (!res.ok) throw new Error(`[creatordeck] fetchActiveAccounts failed (${res.status})`)
    const data = await res.json() as { accounts: SeAccount[] }
    return data.accounts
  }

  async ingestMessages(channelId: string, messages: IncomingChatMessage[]): Promise<void> {
    const res = await fetch(`${env.creatorDeckUrl}/api/internal/youtube-chat`, {
      method: 'POST',
      headers: this.headers,
      body: JSON.stringify({ channelId, messages }),
    })
    if (!res.ok) {
      const body = await res.text().catch(() => '(unreadable)')
      throw new Error(`[creatordeck] ingestMessages failed (${res.status}): ${body}`)
    }
  }

  async updateToken(channelId: string, accessToken: string): Promise<void> {
    const res = await fetch(
      `${env.creatorDeckUrl}/api/internal/se-accounts/${encodeURIComponent(channelId)}/token`,
      {
        method: 'PATCH',
        headers: this.headers,
        body: JSON.stringify({ accessToken }),
      },
    )
    if (!res.ok) {
      const body = await res.text().catch(() => '(unreadable)')
      throw new Error(`[creatordeck] updateToken failed (${res.status}): ${body}`)
    }
  }

  async refreshSeToken(refreshToken: string): Promise<{ accessToken: string; refreshToken: string } | null> {
    const res = await fetch('https://api.streamelements.com/oauth2/token', {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: new URLSearchParams({
        grant_type: 'refresh_token',
        refresh_token: refreshToken,
        client_id: process.env['STREAMELEMENTS_CLIENT_ID'] ?? '',
        client_secret: process.env['STREAMELEMENTS_CLIENT_SECRET'] ?? '',
      }),
    })
    if (!res.ok) return null
    const data = await res.json() as { access_token?: string; refresh_token?: string }
    if (!data.access_token) return null
    return { accessToken: data.access_token, refreshToken: data.refresh_token ?? refreshToken }
  }
}

export const creatorDeckClient = new CreatorDeckClient()