# Spec: [IDEA-4] Alert Configuration System

**Tier:** Tier 2 (`GATES.customAlerts`)
**Status:** Specced, ready to build

---

## Overview

Custom alert overlays for OBS. Streamers configure per-event-type alerts (image/video, sound,
message template) through a dashboard UI. The widget at `/widget/alerts` fetches the config
via widget token and renders alerts using those assets.

Unfair advantages over Streamlabs / StreamElements:
- One overlay handles both Twitch and YouTube events natively — no separate overlays
- Platform badge (Twitch/YouTube logo) on every alert — automatic, no config
- Per-type cards in the dashboard labelled with their platform (Twitch-only / YouTube-only) so streamers know what they're configuring
- Dashboard gracefully handles partially-connected accounts — YouTube-only cards prompt "Connect Twitch" if Twitch isn't linked, and vice versa
- Raid follow batching — collapses follow bursts after a raid into one alert
- Goal milestone alerts — fires a special alert exactly once when a tracked goal target is first crossed (dedup-safe)
- "First of stream" template — special message for the first occurrence of each event type per session

---

## Platform-exclusive event types

| Event type | Platform | Notes |
|---|---|---|
| `sub` | Twitch only | Has sub-kinds: new / resub / community_gift |
| `follow` | Twitch only | |
| `bits` | Twitch only | |
| `raid` | Twitch only | |
| `superchat` | YouTube only | |
| `member` | YouTube only | |

**UI implication:** Per-type cards are always rendered for all 6 event types (so streamers can
pre-configure before connecting a platform). Cards for a platform the user hasn't connected
display a muted "Connect [Platform] to use this alert" notice, and the test button is disabled.
The enabled toggle is still accessible so config is not lost.

---

## Pre-requisite: rebase onto `main`

Before starting implementation, rebase `feature/alerts-engine` onto `main`.
Main has new auth helpers (`src/lib/session-auth.ts`, `src/lib/widget-auth.ts`,
`src/lib/api-auth.ts`) that all new routes must use — do not roll session/widget auth inline.

---

## Database

### New table: `alert_configs`

Unique constraint on `(userId, eventType)`.

| Column | Type | Default | Notes |
|---|---|---|---|
| `id` | uuid | random | PK |
| `userId` | uuid | — | FK → users, cascade delete |
| `eventType` | text | — | sub / follow / bits / raid / superchat / member |
| `enabled` | bool | true | |
| `imageUrl` | text | null | GIF, PNG, JPG, MP4, WebM |
| `soundUrl` | text | null | MP3, WAV, OGG |
| `messageTemplate` | text | null | e.g. `{{user}} just subscribed!` |
| `duration` | int | 6 | seconds the alert is shown |
| `volume` | int | 80 | 0–100 |
| `batchRaidFollows` | bool | false | stored on `follow` row, applies globally |
| `milestoneEnabled` | bool | false | only meaningful for follow / sub / member |
| `milestoneImageUrl` | text | null | |
| `milestoneTemplate` | text | null | e.g. `Goal reached! {{user}} pushed us over {{amount}}!` |
| `firstOfStreamTemplate` | text | null | overrides messageTemplate for first occurrence per session |
| `updatedAt` | timestamp | now | |

### Schema additions to existing tables

Milestone dedup requires a persistent flag so a reconnecting widget cannot re-fire an already-triggered milestone (e.g. if the count dips slightly below the goal and surpasses it again, or the widget reconnects while the count is already at goal).

**`goals` table** — add column:
| Column | Type | Default | Notes |
|---|---|---|---|
| `milestoneReachedAt` | timestamp | null | Set when milestone fires; reset to null if count drops below goal |

**`subGoals` table** — add column:
| Column | Type | Default | Notes |
|---|---|---|---|
| `milestoneReachedAt` | timestamp | null | Same semantics as goals.milestoneReachedAt |

### `LiveEvent` type additions (`src/types/events.ts`)

```ts
isMilestone?: boolean    // annotated server-side when goal target is crossed
batchedCount?: number    // set by SSE batch logic for raid follow bursts
```

---

## Template variables

`alertInterpolate(template, event)` resolves only variables that are valid for the given event
type. Variables that don't apply to the event type resolve to an empty string — they cannot be
inserted via the UI (chips are filtered per type) and are silently stripped if somehow present.

| Variable | Resolves to | Notes |
|---|---|---|
| `{{user}}` | `event.fromUser` | All types |
| `{{amount}}` | `event.amount` | bits, raid, superchat |
| `{{tier}}` | `TWITCH_TIER_LABEL[event.tier]` | sub only — renders "Tier 1" / "Tier 2" / "Tier 3" |
| `{{months}}` | `event.cumulativeMonths ?? event.amount` | sub (resub), member |
| `{{currency}}` | `event.currency` | superchat only |
| `{{level}}` | `event.levelName` | member only |
| `{{count}}` | `event.batchedCount ?? 1` | follow (batched raid follows) |
| `{{subKind}}` | `SUB_KIND_LABEL[event.subKind]` | sub only — "New sub" / "Resub" / "Gift sub" |
| `{{message}}` | `event.message` | sub, bits, superchat |

Per-type variable chips shown in UI (filtered — only show what applies):

| Type | Platform | Available variables |
|---|---|---|
| sub | Twitch | `{{user}}`, `{{tier}}`, `{{months}}`, `{{subKind}}`, `{{message}}` |
| follow | Twitch | `{{user}}`, `{{count}}` |
| bits | Twitch | `{{user}}`, `{{amount}}`, `{{message}}` |
| raid | Twitch | `{{user}}`, `{{amount}}` |
| superchat | YouTube | `{{user}}`, `{{amount}}`, `{{currency}}`, `{{message}}` |
| member | YouTube | `{{user}}`, `{{months}}`, `{{level}}` |

---

## File upload

**Storage:** Vercel Blob (`@vercel/blob`)
**Env var:** `BLOB_READ_WRITE_TOKEN` — read automatically by the Blob SDK; add as optional to `env.ts`:
```ts
blobReadWriteToken: process.env["BLOB_READ_WRITE_TOKEN"] ?? null,
```
This is optional (no `requireEnv`) so the app boots without it in dev; the upload route returns 503 if null.

**Path pattern:** `alerts/{userId}/{kind}/{timestamp}.{ext}`
**Max size:** 5 MB per file
**Allowed types:**
- Image/video: `image/jpeg`, `image/png`, `image/gif`, `image/webp`, `video/mp4`, `video/webm`
- Audio: `audio/mpeg`, `audio/wav`, `audio/ogg`

No cleanup of old blobs in V1. Accept orphans; implement a cleanup cron in V2.

---

## API Routes

### `GET /api/alerts/config`
- Auth: use `getSessionUser()` from `src/lib/session-auth.ts`
- Gate: Tier 2, returns 403 otherwise
- Returns: `AlertConfig[]` for the authed user. Missing event types are filled with in-memory defaults (not written to DB until first save).

### `PUT /api/alerts/config`
- Auth: `getSessionUser()` from `src/lib/session-auth.ts`
- Gate: Tier 2
- Body: `{ eventType: LiveEventType, ...patch }`
- Validates `eventType` against `ALL_TYPES` from `src/lib/event-types.ts`
- Upserts via `onConflictDoUpdate` on `(userId, eventType)`
- Returns: updated row

### `POST /api/alerts/upload`
- Auth: `getSessionUser()` from `src/lib/session-auth.ts`
- Gate: Tier 2
- Body: `multipart/form-data` with `file` (File) and `kind` (`"image"` | `"sound"`)
- Validates type and size client- and server-side
- Returns 503 if `env.blobReadWriteToken` is null
- Uploads to Vercel Blob, returns `{ url: string }`

### `GET /api/widget/alerts/config?token=`
- Auth: use `getWidgetUser()` from `src/lib/widget-auth.ts`
- Gate: checks `user.tier`, returns 403 + `{ error: "tier_required", tier: "tier2" }` if below Tier 2
- Returns: all `AlertConfig` rows for the user keyed by `eventType`, with defaults filled in

---

## SSE Stream Enhancements (`/api/widget/events/stream/route.ts`)

The current SSE emits a flat `LiveEvent[]` — replays and live events merged into one array.
This format is unchanged; the enhancements below add pre-processing before that emit.

### Tier gate — ship as part of the initial system launch

After resolving user from widget token (use `getWidgetUser()` from `widget-auth.ts`):
```ts
if (!hasAccess(user.tier, GATES.customAlerts)) {
  return new Response("Tier required", { status: 403 })
}
```
This is built and deployed as part of the complete alerts system, not as a standalone prior change.
Free/Tier 1 users who had the old widget URL active will be locked out — communicate in release notes.

### Config load on connect
Load `alertConfigsRepository.findByUserIdAsMap(user.id)` once on SSE connect, stored in closure.
Not refreshed per-poll — stale config until widget reconnects (acceptable for V1).

### Raid follow batching
```ts
let lastRaidAt: Date | null = null
const BATCH_WINDOW_MS = 30_000
```

On each poll:
1. If any emitted event is `type === "raid"`, record `lastRaidAt = new Date()`
2. Check `configMap.get("follow")?.batchRaidFollows`
3. If enabled and within `BATCH_WINDOW_MS` of `lastRaidAt`:
   - Separate follow events from others
   - If multiple follows, emit one synthetic follow event:
     - `batchedCount = follows.length`
     - `fromUser = "${follows.length} new followers"`
     - Use first follow's `id` and `occurredAt`
4. Otherwise emit follows individually

Extract into a pure `batchFollowEvents(events, lastRaidAt, windowMs)` function for testability.

### Milestone annotation

**What triggers a milestone:** when the running count of events of a given type first crosses
the configured goal target. Fires exactly once per "crossing" — safe against:
- Widget reconnects (state persisted in DB via `milestoneReachedAt`)
- Count dipping below goal and recovering (reset mechanism)

**Count sources:**
- `sub`: `subGoals.initialCount + COUNT(sub_events WHERE broadcasterId = X)` vs `subGoals.goal`
- `follow`: `COUNT(follow_events WHERE broadcasterId = X)` vs `goals.goal WHERE type = 'twitch_follow'`
- `member`: `COUNT(yt_member_events WHERE channelId = X)` vs `goals.goal WHERE type = 'youtube_member'`

**Per-poll logic** for each event of type `follow`, `sub`, or `member`:
1. Load goal row (skip silently if no goal configured)
2. Compute current count
3. If `count >= goal.goal` and `goal.milestoneReachedAt IS NULL`:
   - Mark the last qualifying event in the batch `isMilestone = true`
   - Write `milestoneReachedAt = now()` to the goal row
4. If `count < goal.goal` and `goal.milestoneReachedAt IS NOT NULL`:
   - Reset `milestoneReachedAt = null` (allows re-trigger after a count dip + recovery)
5. If `count >= goal.goal` and `goal.milestoneReachedAt IS NOT NULL`:
   - Already fired — do nothing

At most one `isMilestone = true` per poll batch per type.

---

## Config UI (`/alerts`)

### Page structure (`page.tsx` — server component)
- Gets session via `getSessionUser()`, fetches configs, goals, and linked accounts; passes all to client component
- Below Tier 2: renders inline restricted state (not a redirect) with upgrade link

### Client component (`alerts-client.tsx`)
- State: `Map<LiveEventType, AlertConfig>` with defaults merged in
- Auto-saves on change with 1-second debounce via `PUT /api/alerts/config`
- File upload: calls `POST /api/alerts/upload`, then updates local state with returned URL — this triggers the debounced save, no special handling needed
- Shows a "Saved" indicator

### Global settings card
- Raid batch toggle (reads/writes `batchRaidFollows` on the `follow` config row)

### Per-type card (`alert-type-card.tsx`)

Cards are rendered for all 6 event types. Each card header shows:
- Event type label
- **Platform pill** — "Twitch" or "YouTube" coloured badge (use the existing `ALERT_CONFIG` color per type from `alerts.types.ts` as fill; no separate `PLATFORM_COLOR` constant needed)
- Enabled toggle (always active, even when platform not connected)

If the card's platform is not connected (no linked account for that provider), render a muted notice:
`"Connect [Platform] to test this alert"` — but do not hide or disable the config fields.

Sections per event type:
1. **Header** — event type label + platform pill + enabled toggle
2. **Image** — file input (image/*, video/mp4, video/webm), preview (`<img>` or `<video>`), clear button
3. **Sound** — file input (audio/*), `<audio>` preview player, volume slider (0–100), clear button
4. **Template** — textarea with variable hint chips. Clicking a chip inserts the variable at cursor position. **Chips are filtered to only show variables valid for this event type** — it must be impossible to insert an inapplicable variable. Falls back to `alertSubtitle()` rendering when empty.
5. **Duration** — slider 1–30 seconds
6. **First of stream** — secondary textarea for `firstOfStreamTemplate` (same chip rules as template)
7. **Milestone** (only for follow / sub / member, only if a goal exists for that type) — toggle, image upload, template input (same chip rules)
8. **Test button** — POSTs a synthetic `LiveEvent` to `POST /api/events/replay`. Disabled (with tooltip) if the platform for this event type is not connected. UI note: "Widget must be open to preview."

---

## Widget Overlay Enhancements (`/widget/alerts`)

### On mount (ordering matters)
1. Fetch `GET /api/widget/alerts/config?token=`, store as `Map<LiveEventType, AlertConfig>` in a ref
2. If 403 → set `tier2Blocked = true`, render `<RestrictedOverlay>` — do **not** connect SSE
3. On success → connect SSE

The config fetch is awaited before SSE connects. While fetching (and while the widget has no current alert), the page renders transparent/empty — no loading spinner needed for an OBS overlay.

### Per-alert rendering
In `showNext()`:
1. Look up config for `next.type`
2. If `config.enabled === false` → dequeue silently, call `showNext()` again
3. Apply `config.duration` as hold duration (replaces URL param; URL param kept as fallback for sub-Tier-2 legacy usage)
4. If `config.soundUrl` → `new Audio(url)`, set `volume = config.volume / 100`, call `.play()` — suppress errors silently

### Alert card layout

Replace the current vertical text-only card with an image-first layout:

```
┌─────────────────────────────┐
│  [type label]    [platform] │  ← header row, same as today
│                             │
│      [image / video]        │  ← centered, fills available width
│                             │
│       [username]            │  ← centered text below image
│       [subtitle/template]   │
│       [message if any]      │
│                             │
└── [color accent bar] ───────┘
```

- If **no** `config.imageUrl` set: render username + subtitle/template centered, same visual weight as today
- If `config.imageUrl` set: render image/video centered above text; for mp4/webm use `<video autoPlay loop muted>`
- Text (username, subtitle, message) is centered in both cases
- **Platform badge** — `TwitchLogo` or `YouTubeLogo` icon at ~14×14px in top-right of header row. Always shown, no config.
- **Template** — if `config.messageTemplate` set, render `alertInterpolate(template, event)` as subtitle. Otherwise fall back to `alertSubtitle(event)`.
- **Milestone** — if `event.isMilestone && config.milestoneEnabled`: use `milestoneImageUrl` and `milestoneTemplate` instead of base config fields
- **Batched follow** — `event.batchedCount > 1` is handled naturally by `alertInterpolate` via `{{count}}`

### First-of-stream tracking
```ts
const firstSeenTypes = useRef<Set<LiveEventType>>(new Set())
```
In `showNext()`: if type not in set and `config.firstOfStreamTemplate` is set, use it instead of
`messageTemplate`. Then add type to set. Resets on page/browser source reload.

Note: tracks by widget page lifecycle, not actual stream session start. Tie to `streamSessions` in V2.

---

## Shared component: `RestrictedOverlay`

Used by:
- `/alerts` page — for sub-Tier-2 users
- `/widget/alerts` — when config fetch returns 403

Props: `{ message: string, linkLabel: string, linkHref: string }`
Renders a dark panel with message and a single link.

---

## New / Modified Files

### New
| File | Purpose |
|---|---|
| `src/repositories/alert-configs.repository.ts` | CRUD for alert_configs |
| `src/app/api/alerts/config/route.ts` | GET/PUT — session auth |
| `src/app/api/alerts/upload/route.ts` | POST — Vercel Blob upload |
| `src/app/api/widget/alerts/config/route.ts` | GET — widget token auth |
| `src/app/alerts/page.tsx` | Config page server component |
| `src/app/alerts/alerts-client.tsx` | Config UI client component |
| `src/app/alerts/alert-type-card.tsx` | Per-type card |
| `src/components/restricted-overlay.tsx` | Shared restricted state component |

### Modified
| File | Change |
|---|---|
| `src/lib/schema.ts` | Add `alertConfigs` table; add `milestoneReachedAt` to `goals` and `subGoals` |
| `src/lib/env.ts` | Add optional `blobReadWriteToken` (no requireEnv — null if absent) |
| `src/types/entities.ts` | Add `AlertConfig`, `InsertAlertConfig` via Drizzle `$inferSelect`/`$inferInsert` |
| `src/types/events.ts` | Add `isMilestone?`, `batchedCount?` to `LiveEvent` |
| `src/repositories/index.ts` | Export `alertConfigsRepository` |
| `src/services/alerts.service.ts` | Add `alertInterpolate()` function |
| `src/services/index.ts` | Export `alertInterpolate` |
| `src/app/api/widget/events/stream/route.ts` | Tier gate, config load, batch logic, milestone annotation |
| `src/app/widget/alerts/page.tsx` | Config fetch, 403 handling, full rendering rework |
| `src/app/dashboard/app-header.tsx` | Add Alerts nav item |

---

## Build Order

0. **Rebase** `feature/alerts-engine` onto `main` — picks up auth helpers
1. **`env.ts`** — add optional `blobReadWriteToken`
2. **Install** `@vercel/blob`
3. **Schema** — add `alertConfigs` table + `milestoneReachedAt` on `goals` + `subGoals`; run `drizzle-kit generate` + apply migration
4. **Types** — `entities.ts` (`AlertConfig`, `InsertAlertConfig`) + `events.ts` (`isMilestone?`, `batchedCount?`)
5. **`alert-configs.repository.ts`** + export from `repositories/index.ts`
6. **`alertInterpolate`** in `alerts.service.ts` + export from `services/index.ts`
7. **`GET/PUT /api/alerts/config`**
8. **`POST /api/alerts/upload`**
9. **`GET /api/widget/alerts/config`**
10. **SSE enhancements** — tier gate + config load + batch logic + milestone annotation (all in one commit; deploy together with the rest of the system)
11. **`restricted-overlay.tsx`**
12. **`/alerts` config page** — `page.tsx` + `alerts-client.tsx` + `alert-type-card.tsx`
13. **Nav** — `app-header.tsx`
14. **Widget overlay rework** — `widget/alerts/page.tsx`

---

## Risk Register

| Risk | Severity | Mitigation |
|---|---|---|
| SSE locks out free/Tier 1 users with existing widget URLs on launch | High | Communicate in release notes; deploy all steps together as one release |
| Milestone fires more than once (widget reconnect, count dip) | High | `milestoneReachedAt` in DB prevents re-trigger across sessions; reset on dip allows legitimate re-trigger |
| Milestone count query expensive at poll frequency | Medium | Only runs when batch contains relevant event types; acceptable at current scale |
| `@vercel/blob` not available in dev without Vercel infra | Medium | `env.blobReadWriteToken` null check returns 503 with descriptive error |
| `{{tier}}` raw value is "1000"/"2000"/"3000" — confusing if passed through | Low | `alertInterpolate` runs through `TWITCH_TIER_LABEL`; inapplicable variables resolve to empty string |
| Audio autoplay blocked in regular browser tabs | Low | Suppress errors silently; works natively in OBS |
| Orphaned Blob files when user replaces images | Low | Accept in V1; cleanup cron in V2 |
| Config stale until widget reconnect | Low | Acceptable for V1 |
| `batchRaidFollows` stored on `follow` row conflates global vs per-type | Low | Documented; migrate to `userAlertSettings` in V2 if needed |
| First-of-stream resets on widget reload | Low | Acceptable for V1; tie to `streamSessions` in V2 |