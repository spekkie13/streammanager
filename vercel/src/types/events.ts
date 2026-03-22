export type LiveEventType = "sub" | "follow" | "bits" | "raid"

export type LiveEvent = {
  id: string
  type: LiveEventType
  fromUser: string
  amount: number | null
  occurredAt: string
}