import type {
  users,
  subEvents,
  followEvents,
  cheerEvents,
  raidEvents,
  streamSessions,
  subGoals,
  waitlist,
  eventsubSubscriptions,
} from "@/lib/schema"

export type User = typeof users.$inferSelect
export type InsertUser = typeof users.$inferInsert

export type SubEvent = typeof subEvents.$inferSelect
export type InsertSubEvent = typeof subEvents.$inferInsert

export type FollowEvent = typeof followEvents.$inferSelect
export type InsertFollowEvent = typeof followEvents.$inferInsert

export type CheerEvent = typeof cheerEvents.$inferSelect
export type InsertCheerEvent = typeof cheerEvents.$inferInsert

export type RaidEvent = typeof raidEvents.$inferSelect
export type InsertRaidEvent = typeof raidEvents.$inferInsert

export type StreamSession = typeof streamSessions.$inferSelect
export type InsertStreamSession = typeof streamSessions.$inferInsert

export type SubGoal = typeof subGoals.$inferSelect
export type InsertSubGoal = typeof subGoals.$inferInsert

export type WaitlistEntry = typeof waitlist.$inferSelect
export type InsertWaitlistEntry = typeof waitlist.$inferInsert

export type EventSubSubscription = typeof eventsubSubscriptions.$inferSelect
export type InsertEventSubSubscription = typeof eventsubSubscriptions.$inferInsert