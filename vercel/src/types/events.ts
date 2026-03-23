export type LiveEventType = "sub" | "follow" | "bits" | "raid" | "superchat" | "member"

export type LiveEvent = {
  id: string
  type: LiveEventType
  fromUser: string
  amount: number | null
  currency?: string | null   // superchat only — ISO 4217 code e.g. "USD"
  occurredAt: string
}