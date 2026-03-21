# Stream Manager — Product Backlog

## Architectuur overzicht

```
Next.js app (gedeployed op Vercel)
├── Twitch OAuth (NextAuth)           ✅ gereed
├── Google/YouTube OAuth (NextAuth)   🔴 nog te bouwen  [Epic 6]
├── Twitch EventSub webhooks          ✅ gereed (subs only)
├── YouTube Live Chat poller          🔴 nog te bouwen  [Epic 6]
├── Neon DB (Drizzle ORM)             ✅ gereed
├── Web dashboard                     🔴 nog te bouwen
└── Config & analytics API            🔴 nog te bouwen

C# Desktop app (lokaal)
├── OBS websocket                     ✅ gereed
├── Spotify API                       ✅ gereed
└── Pollt Next.js API                 ✅ gereed
```

> **Bestaande schema tabellen:** `users`, `sub_events`, `sub_goals`, `eventsub_subscriptions`
> **ORM:** Drizzle ORM — schema wijzigingen in `src/lib/schema.ts`, migreren met `npx drizzle-kit push`
>
> **YouTube vs Twitch architectuurverschil:** Twitch stuurt events via webhooks (EventSub). YouTube heeft geen equivalent — live chat events moeten gepolled worden via de YouTube Data API v3. De poller draait als Vercel Cron Job (elke minuut) en haalt `liveChatMessages` op voor actieve uitzendingen. Super Chats en memberships zitten als speciale message types in de chat response.

---

## Aanbevolen volgorde

```
Week 1:  Epic 1 — Events opslaan & structureren (Twitch)
Week 2:  Epic 2 — Live dashboard
Week 3:  Epic 3 — Configuratie beheren (webapp)
Week 4:  Epic 3 — Configuratie beheren (desktop sync) + dashboard afmaken
Week 5:  Epic 6 — YouTube ondersteuning (OAuth, schema, poller)
Week 6:  Epic 6 — YouTube dashboard integratie
Later:   Epic 4 — Analytics (platform-aware)
Later:   Epic 5 — Monetisatie (pas na eerste externe gebruikers)
```

---

## Epic 1 — Events opslaan & structureren

> Fundament voor alles. Subs worden al opgeslagen via EventSub webhooks. Follows, bits en raids ontbreken nog — zowel in het schema als in de webhook handler.

---

### [1.1] Drizzle schema uitbreiden met follow-, bits- en raid-events

**Doel:** Drie nieuwe tabellen toevoegen aan `src/lib/schema.ts` zodat follow-, bits- en raid-events persistent worden opgeslagen, consistent met de bestaande `sub_events` tabel.

**Context:**
- ORM is **Drizzle** (niet Prisma) — schema staat in `src/lib/schema.ts`
- Migreren via `npx drizzle-kit push`
- Bestaand patroon: aparte tabel per event type, met `eventId` (unique) voor deduplicatie en `broadcasterId` + `occurredAt` op elke tabel

**Claude Code prompt:**
```
I have a Next.js app with Drizzle ORM and Neon DB. The schema is in src/lib/schema.ts.
There is an existing sub_events table I use as a reference pattern — it has: id (uuid), broadcasterId (text), eventId (text unique), userId, userLogin, userDisplayName, occurredAt (timestamp), createdAt (timestamp).

Add three new tables to schema.ts following the same pattern:

followEvents ("follow_events"):
- id (uuid, primaryKey, defaultRandom)
- broadcasterId (text, notNull)
- eventId (text, unique, notNull) — Twitch EventSub message ID
- userId (text) — follower's Twitch ID
- userLogin (text)
- userDisplayName (text)
- occurredAt (timestamp, notNull)
- createdAt (timestamp, defaultNow, notNull)

cheerEvents ("cheer_events"):
- id (uuid, primaryKey, defaultRandom)
- broadcasterId (text, notNull)
- eventId (text, unique, notNull)
- userId (text, nullable — can be anonymous)
- userLogin (text, nullable)
- userDisplayName (text, nullable)
- bits (integer, notNull)
- message (text, nullable)
- isAnonymous (boolean, notNull, default false)
- occurredAt (timestamp, notNull)
- createdAt (timestamp, defaultNow, notNull)

raidEvents ("raid_events"):
- id (uuid, primaryKey, defaultRandom)
- broadcasterId (text, notNull) — the receiving broadcaster
- eventId (text, unique, notNull)
- fromBroadcasterId (text, notNull)
- fromBroadcasterLogin (text, notNull)
- fromBroadcasterDisplayName (text, notNull)
- viewerCount (integer, notNull)
- occurredAt (timestamp, notNull)
- createdAt (timestamp, defaultNow, notNull)

Show the additions to schema.ts and the drizzle-kit push command.
```

---

### [1.2] Stream sessies bijhouden

**Doel:** Automatisch een sessie aanmaken als de stream live gaat en afsluiten als de stream offline gaat, via EventSub `channel.stream.online` / `channel.stream.offline` events.

**Context:**
- Geen polling — detectie loopt via de bestaande EventSub webhook infrastructuur (`/api/webhook`)
- Registratie van nieuwe event types loopt via `src/lib/twitch.ts` (`SUB_TYPES` array) en `/api/register-subscriptions`
- `channel.stream.online` en `channel.stream.offline` gebruiken alleen `broadcaster_user_id` als condition

**Claude Code prompt:**
```
I have a Next.js app with Drizzle ORM and Neon DB. Schema is in src/lib/schema.ts.
Events from Twitch arrive via EventSub webhooks at /api/webhook (not polling).
New EventSub subscription types are registered in src/lib/twitch.ts in the SUB_TYPES array.

1. Add a stream_sessions table to schema.ts:
   - id (uuid, primaryKey, defaultRandom)
   - broadcasterId (text, notNull)
   - startedAt (timestamp, notNull)
   - endedAt (timestamp, nullable)
   - createdAt (timestamp, defaultNow, notNull)

2. In src/lib/twitch.ts, add to SUB_TYPES:
   - { type: "channel.stream.online", version: "1" }
   - { type: "channel.stream.offline", version: "1" }

3. In /api/webhook/route.ts, handle the two new subscription types:
   - channel.stream.online → insert a new stream_sessions row (only if no open session exists for this broadcasterId)
   - channel.stream.offline → set endedAt on the open session for this broadcasterId

Show the schema addition, the twitch.ts change, and the webhook handler additions.
Run: npx drizzle-kit push
```

---

### [1.3] Webhook handler uitbreiden voor follows, bits en raids

**Doel:** De drie nieuwe event types registreren bij Twitch EventSub en de webhook handler uitbreiden om ze te persisteren in de nieuwe tabellen.

**Context:**
- Registratie: voeg types toe aan `SUB_TYPES` in `src/lib/twitch.ts`
- `channel.follow` (version **2**) vereist zowel `broadcaster_user_id` als `moderator_user_id` in de condition — gebruik de `broadcasterId` voor beide
- `channel.cheer` en `channel.raid` gebruiken alleen `broadcaster_user_id`
- Webhook handler staat in `/api/webhook/route.ts` — volg het bestaande patroon: insert met `onConflictDoNothing()` op `eventId`
- De tabellen `follow_events`, `cheer_events`, `raid_events` zijn aangemaakt in [1.1]

**Claude Code prompt:**
```
I have a Next.js app with Drizzle ORM, Neon DB, and a Twitch EventSub webhook setup.

Current state:
- src/lib/twitch.ts has a SUB_TYPES array and registerEventSubSubscriptions() function
- /api/webhook/route.ts handles channel.subscribe, channel.subscription.message, channel.subscription.gift
- New tables follow_events, cheer_events, raid_events exist in schema.ts (from a previous step)

Do two things:

1. In src/lib/twitch.ts, add to SUB_TYPES:
   - { type: "channel.follow", version: "2" }
     Note: channel.follow v2 requires condition { broadcaster_user_id, moderator_user_id } — pass broadcasterId for both
   - { type: "channel.cheer", version: "1" }
   - { type: "channel.raid", version: "1" }
     Note: channel.raid uses condition { to_broadcaster_user_id } instead of broadcaster_user_id

   The registerEventSubSubscriptions function builds the condition as { broadcaster_user_id: broadcasterId } for all types.
   Update it to handle these exceptions: channel.follow needs { broadcaster_user_id, moderator_user_id }, channel.raid needs { to_broadcaster_user_id }.

2. In /api/webhook/route.ts, add handlers for:
   - channel.follow → insert into followEvents (eventId = messageId, userId, userLogin, userDisplayName, occurredAt)
   - channel.cheer → insert into cheerEvents (eventId, userId/userLogin/userDisplayName nullable if anonymous, bits, message, isAnonymous, occurredAt)
   - channel.raid → insert into raidEvents (eventId, fromBroadcasterId, fromBroadcasterLogin, fromBroadcasterDisplayName, viewerCount, occurredAt)

All inserts use .onConflictDoNothing() on eventId for deduplication.
Show the full updated twitch.ts and the additions to webhook/route.ts.
```

---

## Epic 2 — Live dashboard

> Het demo-moment van de tool. Één pagina die alles laat zien wat er tijdens een stream gebeurt.

---

### [2.1] Realtime events naar de frontend

**Doel:** Nieuwe Twitch events direct naar de dashboardpagina streamen zonder dat de gebruiker hoeft te refreshen.

**Context:**
- Events zitten verspreid over meerdere tabellen: `sub_events`, `follow_events`, `cheer_events`, `raid_events`
- Authenticatie loopt via NextAuth (`getServerSession`)
- Drizzle ORM voor queries

**Claude Code prompt:**
```
I have a Next.js app with Drizzle ORM and Neon DB. Users authenticate via NextAuth (Twitch OAuth).
Twitch events are stored in four tables: sub_events, follow_events, cheer_events, raid_events.
All tables have a broadcasterId (text) and occurredAt (timestamp) column.

Set up a Server-Sent Events (SSE) API route at /api/events/stream that:
1. Authenticates the user via getServerSession — uses session.twitchId as broadcasterId
2. Every 3 seconds, queries all four event tables for rows newer than the last sent timestamp
3. Normalises each event into a shared shape: { id, type, fromUser, amount, occurredAt }
   - sub_events: type = "sub", fromUser = userDisplayName, amount = giftCount
   - follow_events: type = "follow", fromUser = userDisplayName, amount = null
   - cheer_events: type = "bits", fromUser = userDisplayName (or "anonymous"), amount = bits
   - raid_events: type = "raid", fromUser = fromBroadcasterDisplayName, amount = viewerCount
4. Streams new events to the client as SSE messages in JSON format

Also show a React hook (useStreamEvents) that connects to this SSE route and returns a live array of events.
```

---

### [2.2] Dashboard pagina

**Doel:** Een overzichtspagina die live support events toont, plus basisstatistieken van de huidige sessie.

**Claude Code prompt:**
```
I have a Next.js app with:
- A useStreamEvents hook that returns a live array of Twitch events via SSE
- Drizzle ORM with Neon DB: tables sub_events, follow_events, cheer_events, raid_events, stream_sessions
- Twitch OAuth via NextAuth

Build a /dashboard page that shows:
1. A live feed of the last 20 support events (follow, sub, bits, raid) with type icon, fromUser and timestamp
2. Total followers and subscribers (fetched from Twitch API using session.accessToken)
3. Current session duration (startedAt from the open stream_sessions row for this broadcaster)

Use Tailwind CSS for styling. Keep it clean and dark-themed.
```

---

### [2.3] OBS & Spotify widget in dashboard

**Doel:** De huidige OBS scene en het spelende Spotify-nummer tonen in het dashboard, opgehaald vanuit de lokale C# desktop app.

**Claude Code prompt:**
```
I have a Next.js dashboard page. My local C# desktop app runs on localhost and has a small HTTP API.
Add two widgets to the dashboard:
1. "Now Playing" — fetches current Spotify track from http://localhost:PORT/spotify/current every 10 seconds and shows title + artist
2. "Current Scene" — fetches active OBS scene from http://localhost:PORT/obs/scene every 5 seconds and shows the scene name

Handle the case where the desktop app is not running (show "Desktop app offline" instead of crashing).
Use client-side fetching (useEffect + setInterval). Tailwind CSS styling.
```

---

## Epic 3 — Configuratie beheren

> Zorgt voor retentie — instellingen worden opgeslagen in de webapp zodat de desktop app ze kan ophalen.

---

### [3.1] Config schema in Neon

**Doel:** Een tabel aanmaken waar de configuratie van de gebruiker (commands, triggers, macro's) als JSON wordt opgeslagen.

**Claude Code prompt:**
```
I have a Next.js app with Drizzle ORM and Neon DB. Schema is in src/lib/schema.ts.
Users authenticate via Twitch OAuth (NextAuth). The users table has id (uuid) and twitchId (text).

Add a user_configs table to schema.ts:
- id (uuid, primaryKey, defaultRandom)
- userId (uuid, unique, references users.id)
- commands (json — array of chat command objects)
- triggers (json — array of event trigger objects)
- macros (json — array of macro objects)
- updatedAt (timestamp, defaultNow, notNull)

Show the schema addition and run: npx drizzle-kit push
```

---

### [3.2] Config API routes

**Doel:** API routes waarmee de webapp (en later de desktop app) de configuratie kan ophalen en opslaan.

**Claude Code prompt:**
```
I have a Next.js app with Drizzle ORM, Neon DB and NextAuth (Twitch OAuth).
The session contains session.twitchId. The users table has twitchId and id (uuid).
There is a user_configs table with userId (references users.id), commands, triggers, macros (all json).

Build two API routes using Next.js App Router:

GET /api/config
- Authenticate user via getServerSession
- Look up user by twitchId, then fetch their user_configs row
- Return the config (or empty defaults { commands: [], triggers: [], macros: [] } if none exists)

POST /api/config
- Authenticate user via getServerSession
- Accept JSON body { commands, triggers, macros }
- Upsert the user_configs row (insert or update on userId conflict)

Show the full route handlers.
```

---

### [3.3] Configuratie UI in de webapp

**Doel:** Een instellingenpagina waar gebruikers hun commands en triggers kunnen bekijken, toevoegen en verwijderen.

**Claude Code prompt:**
```
I have a Next.js app with an API route GET/POST /api/config that returns and saves user config as JSON.
The config has three sections: commands (array), triggers (array), macros (array).

Build a /settings page with three tabs (Commands, Triggers, Macros).
Each tab shows a table of existing items with a delete button per row, and a simple form to add a new item.

For Commands, the fields are: trigger (e.g. !hello), response (text), cooldown (seconds).
For Triggers, the fields are: event (follow|sub|bits|raid), action (send_chat|switch_scene|play_sound), value (string).
For Macros, the fields are: name, actions (comma-separated list of action IDs).

Use Tailwind CSS. Load config on mount, save on every add/delete.
```

---

### [3.4] Desktop app haalt config op uit de webapp

**Doel:** De C# desktop app haalt bij opstarten de configuratie op uit de Next.js API in plaats van lokale hardcoded instellingen.

**Claude Code prompt:**
```
I have a C# desktop app that currently uses hardcoded configuration for chat commands, event triggers and macros.
I have a Next.js API at https://my-app.com/api/config that returns user config as JSON (requires an Authorization: Bearer <token> header).

Add a ConfigService to the C# app that:
1. Reads an API token from a local config file (appsettings.json)
2. On startup, fetches the config from the API using HttpClient
3. Deserializes the JSON into typed C# classes (Command, Trigger, Macro)
4. Exposes the config to other services via dependency injection

Show the full ConfigService implementation and the model classes.
```

---

## Epic 4 — Analytics

> De killer feature die het abonnement rechtvaardigt. Streamers kunnen terugkijken op elke sessie.

---

### [4.1] Analytics API route

**Doel:** Een API route die geaggregeerde statistieken teruggeeft per dag en per sessie.

**Claude Code prompt:**
```
I have a Next.js app with Drizzle ORM and Neon DB. Users authenticate via NextAuth (session.twitchId = broadcasterId).
Tables: sub_events, follow_events, cheer_events, raid_events (all with broadcasterId, occurredAt), stream_sessions (broadcasterId, startedAt, endedAt).

Build an API route GET /api/analytics that:
- Authenticates the user via getServerSession
- Accepts query params: range (7d | 30d | 90d) and optionally sessionId
- Returns:
  - Total counts per event type (follow, sub, bits, raid) for the range
  - Events grouped by day (for chart rendering): { date, follows, subs, bits, raids }
  - List of sessions in the range with startedAt, endedAt, and per-type totals

Return as JSON. Use Drizzle queries (group by day using SQL date_trunc where needed).
```

---

### [4.2] Analytics pagina

**Doel:** Een overzichtspagina met grafieken van subs, follows, bits en raids over tijd.

**Claude Code prompt:**
```
I have a Next.js app with an API route /api/analytics that returns event totals grouped by day and by session.
Build an /analytics page with:
1. A date range selector (7 days / 30 days / 90 days)
2. Four summary cards: total follows, subs, bits, raids for the selected range
3. A bar chart (Recharts) showing daily event counts, with each event type as a stacked bar
4. A table of recent stream sessions with columns: date, duration, follows, subs, bits, raids

Use Tailwind CSS, dark theme consistent with the dashboard.
```

---

### [4.3] Sessie detail pagina

**Doel:** Een detailpagina per stream sessie met alle events in chronologische volgorde.

**Claude Code prompt:**
```
I have a Next.js app with Drizzle ORM and Neon DB.
Tables: stream_sessions (id, broadcasterId, startedAt, endedAt), sub_events, follow_events, cheer_events, raid_events (all with broadcasterId, occurredAt).

Build a /analytics/[sessionId] page that shows:
1. Session metadata: date, start time, end time, total duration
2. Summary cards: follows, subs, bits, raids totals for this session
3. A chronological timeline of all events in this session (time, type, fromUser, amount)
   - Filter each table by broadcasterId and occurredAt between startedAt and endedAt
   - Merge and sort all results by occurredAt

Fetch data server-side using Drizzle. Use Tailwind CSS, dark theme.
```

---

## Epic 5 — Monetisatie

> Pas aanpakken als epics 1–4 solide staan en je eerste externe gebruikers hebt.

---

### [5.1] Stripe abonnement integreren

**Doel:** Gebruikers kunnen een maandelijks abonnement afsluiten via Stripe Checkout.

**Claude Code prompt:**
```
I have a Next.js app with NextAuth (Twitch OAuth) and a users table in Neon (Drizzle ORM).
The users table is in src/lib/schema.ts and has id (uuid), twitchId (text), twitchLogin (text), twitchDisplayName (text), apiKey (text), createdAt (timestamp).

Integrate Stripe for a monthly subscription:
1. Add subscriptionStatus (text, default "free") and stripeCustomerId (text, nullable) to the users table in schema.ts
2. Build an API route POST /api/billing/checkout that creates a Stripe Checkout session and redirects the user
3. Build an API route POST /api/billing/webhook that handles Stripe webhooks:
   - checkout.session.completed → set subscriptionStatus to "pro"
   - customer.subscription.deleted → set subscriptionStatus back to "free"
4. Show the required environment variables and Stripe dashboard setup steps

Use the official Stripe Node.js SDK. Use Drizzle for all DB updates. Run: npx drizzle-kit push
```

---

### [5.2] Feature gates per abonnementslaag

**Doel:** Analytics en instellingen zijn alleen beschikbaar voor Pro gebruikers.

**Claude Code prompt:**
```
I have a Next.js app where users have a subscriptionStatus field ("free" | "pro") in the users table (Drizzle ORM, Neon DB).
The session contains session.twitchId — use this to look up the user's subscriptionStatus from the DB.

Add a subscription check to the following:
1. /analytics page — redirect free users to /billing with a message explaining the feature requires Pro
2. /api/analytics route — return 403 with { error: "pro_required" } for free users
3. /settings page — show a paywall banner for free users, disable the save button

Show the middleware or page-level checks needed for Next.js App Router.
```

---

### [5.3] Billing pagina

**Doel:** Gebruikers kunnen hun abonnement bekijken en beheren.

**Claude Code prompt:**
```
I have a Next.js app with Stripe integration. Users have a subscriptionStatus ("free" | "pro") and stripeCustomerId in the Drizzle users table.
Build a /billing page that shows:
1. Current plan (Free or Pro) with a description of what's included
2. For Pro users: next billing date (fetched from Stripe API) and a "Manage subscription" button that opens Stripe Customer Portal
3. For Free users: a pricing card with a "Upgrade to Pro — €10/month" button that triggers the Stripe Checkout flow

Use Tailwind CSS, dark theme.
```

---

*Backlog gecorrigeerd op basis van werkelijke codebase: Next.js + Neon DB + **Drizzle ORM** + NextAuth Twitch OAuth (webapp) en C# desktop app met OBS, Spotify en Twitch integraties. EventSub webhooks (geen polling). Bestaande tabellen: `users`, `sub_events`, `sub_goals`, `eventsub_subscriptions`.*
