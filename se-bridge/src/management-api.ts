import { Router, Request, Response, NextFunction } from 'express'
import { env } from './env'
import type { ChannelRegistry } from './registry'

function requireSecret(req: Request, res: Response, next: NextFunction): void {
  const auth = req.headers['authorization']
  if (auth !== `Bearer ${env.northflankWebhookSecret}`) {
    res.status(401).json({ error: 'Unauthorized' })
    return
  }
  next()
}

export function buildManagementRouter(registry: ChannelRegistry): Router {
  const router = Router()

  router.use(requireSecret)

  router.post('/channels', (req: Request, res: Response) => {
    const { channelId, accessToken, refreshToken } = req.body as {
      channelId?: string
      accessToken?: string
      refreshToken?: string
    }
    if (!channelId || !accessToken || !refreshToken) {
      res.status(400).json({ error: 'Missing channelId, accessToken, or refreshToken' })
      return
    }
    registry.register(channelId, accessToken, refreshToken)
    res.json({ ok: true })
  })

  router.delete('/channels/:channelId', (req: Request, res: Response) => {
    registry.deregister(req.params['channelId'])
    res.json({ ok: true })
  })

  router.get('/channels', (_req: Request, res: Response) => {
    res.json({ channels: registry.list() })
  })

  router.get('/health', (_req: Request, res: Response) => {
    res.json({ ok: true, channels: registry.size() })
  })

  return router
}
