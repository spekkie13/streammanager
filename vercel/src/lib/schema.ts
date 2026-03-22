import { pgTable, text, timestamp, integer, uuid, boolean } from "drizzle-orm/pg-core"

export const users = pgTable("users", {
  id: uuid("id").defaultRandom().primaryKey(),
  twitchId: text("twitch_id").unique(),         // nullable: YouTube-only users have no Twitch account
  twitchLogin: text("twitch_login"),
  twitchDisplayName: text("twitch_display_name"),
  accessToken: text("access_token"),
  refreshToken: text("refresh_token"),
  apiKey: text("api_key").unique().notNull(),
  createdAt: timestamp("created_at").defaultNow().notNull(),
})

export const subEvents = pgTable("sub_events", {
  id: uuid("id").defaultRandom().primaryKey(),
  broadcasterId: text("broadcaster_id").notNull(),
  eventId: text("event_id").unique().notNull(),
  userId: text("user_id"),
  userLogin: text("user_login"),
  userDisplayName: text("user_display_name"),
  gifterId: text("gifter_id"),
  gifterLogin: text("gifter_login"),
  gifterDisplayName: text("gifter_display_name"),
  tier: text("tier").notNull(),
  kind: text("kind").notNull(),
  giftCount: integer("gift_count").default(1),
  cumulativeMonths: integer("cumulative_months"),
  message: text("message"),
  occurredAt: timestamp("occurred_at").notNull(),
  createdAt: timestamp("created_at").defaultNow().notNull(),
})

export const subGoals = pgTable("sub_goals", {
  broadcasterId: text("broadcaster_id").primaryKey(),
  goal: integer("goal").notNull().default(100),
  endsAt: timestamp("ends_at"),
  updatedAt: timestamp("updated_at").defaultNow().notNull(),
})

export const eventsubSubscriptions = pgTable("eventsub_subscriptions", {
  id: text("id").primaryKey(),
  broadcasterId: text("broadcaster_id").notNull(),
  type: text("type").notNull(),
  status: text("status").notNull(),
  createdAt: timestamp("created_at").defaultNow().notNull(),
})

export const followEvents = pgTable("follow_events", {
  id: uuid("id").defaultRandom().primaryKey(),
  broadcasterId: text("broadcaster_id").notNull(),
  eventId: text("event_id").unique().notNull(),
  userId: text("user_id"),
  userLogin: text("user_login"),
  userDisplayName: text("user_display_name"),
  occurredAt: timestamp("occurred_at").notNull(),
  createdAt: timestamp("created_at").defaultNow().notNull(),
})

export const cheerEvents = pgTable("cheer_events", {
  id: uuid("id").defaultRandom().primaryKey(),
  broadcasterId: text("broadcaster_id").notNull(),
  eventId: text("event_id").unique().notNull(),
  userId: text("user_id"),
  userLogin: text("user_login"),
  userDisplayName: text("user_display_name"),
  bits: integer("bits").notNull(),
  message: text("message"),
  isAnonymous: boolean("is_anonymous").notNull().default(false),
  occurredAt: timestamp("occurred_at").notNull(),
  createdAt: timestamp("created_at").defaultNow().notNull(),
})

export const raidEvents = pgTable("raid_events", {
  id: uuid("id").defaultRandom().primaryKey(),
  broadcasterId: text("broadcaster_id").notNull(),
  eventId: text("event_id").unique().notNull(),
  fromBroadcasterId: text("from_broadcaster_id").notNull(),
  fromBroadcasterLogin: text("from_broadcaster_login").notNull(),
  fromBroadcasterDisplayName: text("from_broadcaster_display_name").notNull(),
  viewerCount: integer("viewer_count").notNull(),
  occurredAt: timestamp("occurred_at").notNull(),
  createdAt: timestamp("created_at").defaultNow().notNull(),
})

export const streamSessions = pgTable("stream_sessions", {
  id: uuid("id").defaultRandom().primaryKey(),
  broadcasterId: text("broadcaster_id").notNull(),
  startedAt: timestamp("started_at").notNull(),
  endedAt: timestamp("ended_at"),
  createdAt: timestamp("created_at").defaultNow().notNull(),
})

export const waitlist = pgTable("waitlist", {
  id: uuid("id").defaultRandom().primaryKey(),
  email: text("email").unique().notNull(),
  twitchLogin: text("twitch_login"),
  createdAt: timestamp("created_at").defaultNow().notNull(),
})
