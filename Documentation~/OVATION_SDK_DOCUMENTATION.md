# Ovation Unity SDK — Complete Documentation

> **Purpose of this document:** This is a comprehensive reference for any agent or developer who needs to understand how the Ovation Unity SDK works, how it maps to the Ovation API, and how game developers use it. It is intended to be handed to the agent responsible for the API and Authority Portal so they can write accurate helper code snippets, documentation examples, and portal integration guides.

---

## Table of Contents

1. [Platform Overview](#1-platform-overview)
2. [SDK at a Glance](#2-sdk-at-a-glance)
3. [Installation & Setup](#3-installation--setup)
4. [Initialization](#4-initialization)
5. [Authentication & API Keys](#5-authentication--api-keys)
6. [Player Identity System](#6-player-identity-system)
7. [Achievement Operations](#7-achievement-operations)
8. [Slot & Equipment Operations](#8-slot--equipment-operations)
9. [Asset System](#9-asset-system)
10. [Achievement Toast UI](#10-achievement-toast-ui)
11. [Offline Queue](#11-offline-queue)
12. [Error Handling](#12-error-handling)
13. [Events System](#13-events-system)
14. [Configuration Reference](#14-configuration-reference)
15. [API Endpoint Mapping](#15-api-endpoint-mapping)
16. [Data Models Reference](#16-data-models-reference)
17. [Editor Tools](#17-editor-tools)
18. [Architecture & Internals](#18-architecture--internals)
19. [Code Snippet Cookbook](#19-code-snippet-cookbook)

---

## 1. Platform Overview

**Ovation** is a universal achievement platform — a centralized system for issuing, tracking, and verifying achievements across games, apps, and platforms. Think cross-platform Xbox Achievements or Steam Achievements that any organization can integrate with.

### Core Concepts

| Concept | Description |
|---------|-------------|
| **Authority** | Top-level tenant (game studio, brand, platform). Everything is scoped to an authority. Authenticates via API keys. |
| **Achievement** | Something a player can earn. Identified by an immutable **slug** (e.g., `first-blood`). Has a display name, optional description, repeatable flag, hidden flag, rarity percentage, and bound assets. Uses soft delete (archived flag). |
| **Player** | Starts as anonymous (just a UUID). The SDK auto-creates players. Players can have **external IDs** per authority (Steam ID, Xbox gamertag, etc.). Players exist globally across all authorities — this is the foundation for cross-game profiles. |
| **Asset** | A file (image) or text content bound to a specific slot. Assets are **versioned** — uploading a new file creates a new version. Bound to achievements via `slot_assets`. |
| **Slot** | A global customization point defined by Ovation (not per-authority). Defines "where can a player display something?" Examples: `badge`, `spray`, `emoji`, `avatar_frame`, `profile_banner`, `player_icon`, `nameplate_title`. |

### Standard Phase 1 Slots

| Slot | Asset Type | Dimensions | Max Size | Notes |
|------|-----------|-----------|----------|-------|
| `badge` | image | 512×512 | 256KB | Transparent background required |
| `nameplate_title` | text | — | — | Max 24 chars, alphanumeric + spaces/hyphens/periods |
| `profile_banner` | image | 1200×400 | 512KB | No transparency (background fill) |
| `avatar_frame` | image | 256×256 (inner 192×192) | 128KB | Transparent center for avatar compositing |
| `player_icon` | image | 256×256 | 128KB | Transparency optional |
| `emoji` | image | 128×128 | 64KB | Transparent background required |
| `spray` | image | 512×512 | 256KB | Transparency optional |

---

## 2. SDK at a Glance

- **Package name:** `games.ovation.sdk`
- **Version:** 0.1.0
- **Platform:** Unity 2022.3 LTS+
- **Language:** C#
- **Distribution:** UPM via Git URL
- **Dependencies:** `com.unity.nuget.newtonsoft-json` 3.2.1, `com.unity.textmeshpro` 3.0.6
- **License:** MIT
- **Namespace:** `Ovation` (sub-namespaces: `Ovation.Api`, `Ovation.Models`, `Ovation.Cache`, `Ovation.Queue`, `Ovation.Identity`, `Ovation.UI`, `Ovation.Utils`)

### What Game Developers Get

- **Achievements in one line of code** — `OvationSDK.Unlock("first-blood")`
- **Production backend** — no database, no server, no infrastructure to manage
- **Automatic player identity** — anonymous player IDs created and persisted automatically
- **Offline support** — achievements queue locally when offline, sync when connectivity returns
- **Built-in toast UI** — Steam-quality achievement popups, fully customizable or easy to disable
- **Asset caching** — achievement images downloaded and cached to disk with LRU eviction
- **Dual async API** — both callbacks and async/await for every operation
- **Editor dev tools** — test achievements, inspect player state, reset data without leaving Unity

---

## 3. Installation & Setup

### Install via UPM

In Unity Editor: **Window > Package Manager > + > Add package from git URL**

```
https://github.com/samparks/ovation-unity.git
```

### Two Setup Options

**Option A: Code-only (simplest, no scene setup)**

```csharp
using Ovation;
using Ovation.UI;

async void Start()
{
    await OvationSDK.Init("your-api-key", enableDebugLogging: true);
    AchievementToast.Create(); // Optional: toast popups
}
```

**Option B: Scene-based (inspector-configurable)**

1. Create a config asset: **Assets > Create > Ovation > Config**
2. Add an empty GameObject to scene
3. Add `OvationSDK` component, assign config asset
4. SDK auto-initializes on `Start()`

---

## 4. Initialization

### Static Init (Code-Only)

```csharp
public static async Task Init(string apiKey, string baseUrl = null, bool enableDebugLogging = false)
```

This method:
1. Creates a `[Ovation SDK]` GameObject with `DontDestroyOnLoad`
2. Creates a runtime `OvationConfig` ScriptableObject
3. Sets up the API client, all service layers, identity manager, offline queue, and asset cache
4. Auto-creates an anonymous player via `POST /v1/players` (if `autoManagePlayerId` is true)
5. Stores the player UUID in `PlayerPrefs` under key `Ovation_PlayerId`
6. Flushes any pending offline queue items

**If already initialized:** Updates the API key without re-creating.

### Scene-Based Init

When a `OvationConfig` asset is assigned to the `OvationSDK` component, `InitializeAsync()` is called automatically in `Start()`.

### Key Properties After Init

| Property | Type | Description |
|----------|------|-------------|
| `OvationSDK.IsReady` | `bool` (static) | Whether the SDK is initialized and ready |
| `OvationSDK.Instance` | `OvationSDK` | The singleton instance |
| `OvationSDK.Instance.PlayerId` | `string` | Current player's Ovation UUID |
| `OvationSDK.Instance.IsInitialized` | `bool` | Whether this instance is initialized |
| `OvationSDK.Instance.OfflineQueueCount` | `int` | Items waiting in offline queue |

---

## 5. Authentication & API Keys

### Key Format
```
ovn_{environment}_{random_token}
```

- `ovn_test_*` — creates isolated test data, can be wiped
- `ovn_live_*` — creates real, persistent data

### How the SDK Sends Auth
All API requests include:
```
Authorization: Bearer ovn_live_aB3cD4eF5gH6...
```

### Security Best Practice
API keys should **never** be embedded in game binaries. The SDK supports runtime key injection:

```csharp
// Fetch from your backend at runtime
string apiKey = await YourBackend.FetchOvationKey();
await OvationSDK.Init(apiKey);

// Or update later
OvationSDK.Instance.SetApiKey(newApiKey);
```

The `OvationConfig` inspector field is for **editor testing only**.

### API Base URL
- **Default:** `https://api.ovation.games`
- All endpoints prefixed with `/v1`
- Overridable via `OvationConfig.baseUrlOverride` or the `baseUrl` parameter of `Init()`

---

## 6. Player Identity System

The SDK fully automates player identity management via `PlayerIdentityManager`.

### How It Works

1. **First launch:** SDK calls `POST /v1/players` → gets back a UUID
2. **Stores in `PlayerPrefs`** under key `Ovation_PlayerId`
3. **Subsequent launches:** Reads UUID from `PlayerPrefs`, skips API call
4. **Access:** `OvationSDK.Instance.PlayerId` returns the UUID

### Self-Managed Player IDs

Disable auto-management in config (`autoManagePlayerId = false`), then:

```csharp
OvationSDK.Instance.SetPlayerId("your-known-player-uuid");
```

### External IDs (Platform Linking)

Link platform-specific identifiers to the anonymous Ovation player:

```csharp
// Callback
OvationSDK.Instance.SetExternalId("steam_76561198012345",
    result => Debug.Log($"Linked: {result.ExternalId}"),
    error => Debug.LogError(error.Message)
);

// Async
var result = await OvationSDK.Instance.SetExternalIdAsync("steam_76561198012345");
```

**API call:** `PUT /v1/players/{id}/external-id` with body `{ "external_id": "steam_76561198012345" }`

**Response model — `ExternalIdResponse`:**
| Field | Type | Description |
|-------|------|-------------|
| `PlayerId` | `string` | Player UUID |
| `AuthorityId` | `string` | Authority UUID |
| `ExternalId` | `string` | The external ID that was set |

---

## 7. Achievement Operations

### Issue Achievement (The One-Liner)

```csharp
// Fire-and-forget (simplest)
OvationSDK.Unlock("first-blood");

// Async with result
var result = await OvationSDK.UnlockAsync("first-blood");

// Callback with full control
OvationSDK.Instance.IssueAchievement("first-blood",
    result => {
        if (result.WasNew) Debug.Log("First time!");
        if (result.WasQueued) Debug.Log("Offline, queued for sync");
    },
    error => Debug.LogError(error.Message)
);
```

**API call:** `POST /v1/players/{playerId}/achievements`
**Request body:** `{ "slug": "first-blood" }`
**Optional header:** `Idempotency-Key: unique-request-id`

**Behavior:**
- Returns `201` if new, `200` if already earned
- If network error → automatically queues to offline queue with idempotency key
- If `was_new: true` → fires `OnAchievementEarned` event → triggers toast if enabled
- Async variant throws `OvationException` on API errors (not network errors)

**Response model — `IssueAchievementResult`:**
| Field | Type | JSON Key | Description |
|-------|------|----------|-------------|
| `Slug` | `string` | `slug` | Achievement slug |
| `DisplayName` | `string` | `display_name` | Human-readable name (null if queued) |
| `EarnedAt` | `DateTimeOffset` | `earned_at` | When earned (default if queued) |
| `WasNew` | `bool` | `was_new` | True if first-time unlock |
| `WasQueued` | `bool` | *(client-only)* | True if queued offline |

### List All Achievements

```csharp
// Async
var achievements = await OvationSDK.Instance.GetAchievementsAsync();

// Callback
OvationSDK.Instance.GetAchievements(
    achievements => { /* List<Achievement> */ },
    error => { /* OvationError */ }
);
```

**API call:** `GET /v1/achievements` (auto-paginates all pages)

### Get Single Achievement

```csharp
var achievement = await OvationSDK.Instance.GetAchievementAsync("first-blood");
```

**API call:** `GET /v1/achievements/{slug}`

### Get Player's Earned Achievements

```csharp
var earned = await OvationSDK.Instance.GetPlayerAchievementsAsync();
```

**API call:** `GET /v1/players/{playerId}/achievements` (auto-paginates)

---

## 8. Slot & Equipment Operations

### List Standard Slots

```csharp
var slots = await OvationSDK.Instance.GetStandardSlotsAsync();
```

**API call:** `GET /v1/slots/standard` (no auth required)

### Get All Equipped Assets

```csharp
var equipped = await OvationSDK.Instance.GetEquippedAssetsAsync();
```

**API call:** `GET /v1/slots/equipped?player_id={playerId}`

### Equip Asset

```csharp
var result = await OvationSDK.Instance.EquipAssetAsync(slotId, assetId);
```

**API call:** `POST /v1/slots/{slot_id}/equip`
**Request body:** `{ "player_id": "{playerId}", "asset_id": "{assetId}" }`

### Unequip Asset

```csharp
await OvationSDK.Instance.UnequipAssetAsync(slotId);
```

**API call:** `POST /v1/slots/{slot_id}/unequip`
**Request body:** `{ "player_id": "{playerId}" }`

### Get Equipped Asset for Specific Slot

```csharp
var equipped = await OvationSDK.Instance.GetEquippedAssetAsync("avatar_frame");
```

**API call:** `GET /v1/slots/{slot_name}/equipped/player?player_id={playerId}`

---

## 9. Asset System

### Get Asset Details

```csharp
var asset = await OvationSDK.Instance.GetAssetAsync(assetId);
```

**API call:** `GET /v1/assets/{id}`

### Download & Cache Asset Images

The SDK includes a disk-based image cache with LRU eviction.

```csharp
// As Texture2D (for 3D rendering)
OvationSDK.Instance.LoadAssetTexture(assetUrl, version, texture => {
    renderer.material.mainTexture = texture;
});

// As Sprite (for UI)
OvationSDK.Instance.LoadAssetSprite(assetUrl, version, sprite => {
    image.sprite = sprite;
});

// Async variants
var texture = await OvationSDK.Instance.LoadAssetTextureAsync(url, version);
var sprite = await OvationSDK.Instance.LoadAssetSpriteAsync(url, version);
```

### Cache Details

- **Location:** `Application.persistentDataPath/OvationCache/`
- **Cache key:** SHA256 hash of `"{url}|v{version}"`
- **Index file:** `cache_index.json` (tracks URL, version, file size, last access time)
- **Behavior:**
  - First request: downloads from URL, saves to disk, returns Texture2D/Sprite
  - Subsequent requests: loads from disk cache
  - Version change: re-downloads and overwrites
- **LRU eviction:** When total cache exceeds `maxCacheSizeMB` (default 50MB), oldest-accessed files are deleted first
- **Survives app restarts**

---

## 10. Achievement Toast UI

A built-in, optional popup notification system that fires when achievements are unlocked.

### Setup

```csharp
// Default configuration
AchievementToast.Create();

// Custom configuration
var config = ScriptableObject.CreateInstance<AchievementToastConfig>();
config.position = ToastPosition.TopCenter;
config.displayDuration = 5f;
config.backgroundColor = new Color(0.1f, 0.1f, 0.2f, 0.95f);
AchievementToast.Create(config);
```

Or create a config asset: **Assets > Create > Ovation > Toast Config**

### How It Works

1. Subscribes to `OvationSDK.Instance.OnAchievementEarned`
2. When an achievement fires with `WasNew == true`:
   - Fetches full achievement details (for rarity and slot assets)
   - Creates a procedurally-built UI panel (no prefabs needed)
   - Shows: Ovation icon, "ACHIEVEMENT UNLOCKED" header, achievement name, rarity tier, slot pills
   - Slides in from screen edge, holds for `displayDuration`, slides out
3. Queues multiple toasts sequentially

### Toast Appearance

- **Default:** Dark semi-transparent panel, white text, blue accent
- **Rarity tiers:** Mythic (≤1%), Legendary (≤5%), Rare (≤20%), Uncommon (≤50%), Common (>50%)
- **Slot pills:** Shows which slots the achievement unlocks (e.g., "Badge", "Spray")
- **Canvas:** Screen-space overlay at sort order 9999, scales with screen size (1920×1080 reference)

### Configuration Options (`AchievementToastConfig`)

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `backgroundColor` | `Color` | Dark gray (0.12, 0.12, 0.16, 0.92) | Panel background |
| `titleColor` | `Color` | White | Achievement name color |
| `descriptionColor` | `Color` | Light gray | Secondary text color |
| `accentColor` | `Color` | Blue (0.38, 0.71, 1.0) | Header & pill accent |
| `position` | `ToastPosition` | `TopRight` | Screen position |
| `width` | `float` | 360 | Panel width in canvas units |
| `margin` | `float` | 20 | Margin from screen edge |
| `displayDuration` | `float` | 3.0 | Seconds to show |
| `slideInDuration` | `float` | 0.3 | Slide-in animation time |
| `slideOutDuration` | `float` | 0.3 | Slide-out animation time |
| `sound` | `AudioClip` | null | Optional sound effect |
| `soundVolume` | `float` | 0.5 | Sound volume (0-1) |

### Toast Positions

`TopRight` (default), `TopLeft`, `BottomRight`, `BottomLeft`, `TopCenter`

---

## 11. Offline Queue

When `IssueAchievement` fails due to a **network error** (not 4xx API errors), the request is automatically queued.

### Queue Behavior

1. SDK generates an idempotency key if one wasn't provided
2. Request serialized to `Application.persistentDataPath/OvationQueue/queue.json`
3. Returns `IssueAchievementResult` with `WasQueued = true` to the caller
4. **Flush triggers:**
   - Every `queueFlushIntervalSeconds` (default 60s) via `Update()`
   - Immediately on SDK initialization (for items from previous sessions)
5. **Retry policy:** Up to 5 attempts with exponential backoff: 0s, 60s, 300s, 1800s, 7200s
6. **On success:** Fires `OnQueuedAchievementSynced` event, removes from queue
7. **On network error during flush:** Stops flush (still offline)
8. **On API error during flush:** Increments attempt counter, schedules next retry
9. **Max queue size:** Configurable (default 100). Oldest items dropped when full.
10. **Survives app restarts** (persisted to disk)

### Queued Request Structure (internal)

```json
{
    "slug": "first-blood",
    "player_id": "uuid",
    "idempotency_key": "unique-key",
    "queued_at_utc": "2026-03-15T12:00:00Z",
    "attempt_count": 0,
    "next_retry_utc": "2026-03-15T12:00:00Z"
}
```

---

## 12. Error Handling

### Error Model — `OvationError`

| Field | Type | Description |
|-------|------|-------------|
| `Code` | `string` | Machine-readable error code |
| `Message` | `string` | Human-readable message |
| `HttpStatusCode` | `int` | HTTP status (0 for network errors) |
| `IsNetworkError` | `bool` | True if request never reached server |

### API Error Codes

| HTTP Status | Code | Meaning |
|-------------|------|---------|
| 400 | `invalid_request` | Bad request body or parameters |
| 401 | `authentication_failed` | Missing, invalid, or inactive API key |
| 404 | `achievement_not_found` | Achievement slug doesn't exist for this authority |
| 404 | `player_not_found` | Player ID doesn't exist |
| 404 | `asset_not_found` | Asset ID doesn't exist |
| 404 | `slot_not_found` | Slot doesn't exist |
| 409 | `slug_already_exists` | Achievement slug already taken |
| 409 | `external_id_conflict` | External ID already linked to different player |
| 410 | `achievement_archived` | Trying to issue an archived achievement |

### SDK-Added Error Codes

| Code | Meaning |
|------|---------|
| `network_error` | Request failed due to connectivity (timeout, DNS, etc.) |
| `parse_error` | Server response couldn't be deserialized |
| `unknown_error` | Unexpected error format from server |

### Error Flow

**Callback API:**
- Error passed to `onError` callback
- `OnError` event also fires

**Async API:**
- Throws `OvationException` (wraps `OvationError`)
- `OnError` event also fires
- Exception: network errors on `IssueAchievement` don't throw — they queue and return `WasQueued = true`

### API Error Response Format

```json
{
    "error": {
        "code": "achievement_not_found",
        "message": "No achievement with slug 'typo-slug' exists for this authority."
    }
}
```

---

## 13. Events System

The SDK fires C# events on the `OvationSDK.Instance`:

| Event | Signature | When It Fires |
|-------|-----------|--------------|
| `OnAchievementEarned` | `Action<IssueAchievementResult>` | When `IssueAchievement` returns `WasNew == true`. Does NOT fire for already-earned or queued requests. |
| `OnError` | `Action<OvationError>` | On any API error, in addition to per-call callbacks. Useful for global error logging. |
| `OnQueuedAchievementSynced` | `Action<string, IssueAchievementResult>` | When a previously offline-queued achievement syncs. Parameters: (slug, result). |

### Usage

```csharp
OvationSDK.Instance.OnAchievementEarned += result => {
    Debug.Log($"Achievement unlocked: {result.DisplayName}");
};

OvationSDK.Instance.OnError += error => {
    Analytics.TrackError("ovation", error.Code, error.Message);
};

OvationSDK.Instance.OnQueuedAchievementSynced += (slug, result) => {
    Debug.Log($"Queued '{slug}' synced successfully");
};
```

---

## 14. Configuration Reference

### `OvationConfig` (ScriptableObject)

Created via **Assets > Create > Ovation > Config**.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `apiKey` | `string` | *(empty)* | API key for editor testing. Fetch from backend at runtime. |
| `environment` | `OvationEnvironment` | `Test` | Test or Live environment toggle |
| `baseUrlOverride` | `string` | *(empty)* | Override base URL (default: `https://api.ovation.games`) |
| `enableDebugLogging` | `bool` | `true` | Enable `[Ovation]` prefixed logs |
| `autoManagePlayerId` | `bool` | `true` | Auto-create and persist anonymous player ID |
| `maxQueueSize` | `int` | `100` | Max offline queue items |
| `queueFlushIntervalSeconds` | `float` | `60` | Seconds between queue flush attempts |
| `maxCacheSizeMB` | `int` | `50` | Max asset cache size in MB |

### Runtime Setters

```csharp
config.SetApiKey(string key)
config.SetBaseUrlOverride(string url)
config.SetDebugLogging(bool enabled)
```

---

## 15. API Endpoint Mapping

This is the critical mapping between SDK methods and API endpoints. **All URLs are relative to `{baseUrl}/v1`.**

### Players

| SDK Method | HTTP | Endpoint | Request Body | Response Type |
|-----------|------|----------|-------------|---------------|
| *(auto, on init)* | `POST` | `/players` | *(empty)* | `Player` |
| *(not directly exposed)* | `GET` | `/players/{id}` | — | `Player` |
| *(not directly exposed)* | `GET` | `/players/by-external-id/{external_id}` | — | `Player` |
| `SetExternalId()` | `PUT` | `/players/{id}/external-id` | `{ "external_id": "..." }` | `ExternalIdResponse` |
| `IssueAchievement()` | `POST` | `/players/{id}/achievements` | `{ "slug": "..." }` | `IssueAchievementResult` |
| `GetPlayerAchievements()` | `GET` | `/players/{id}/achievements` | — | `List<PlayerAchievement>` *(paginated)* |

### Achievements

| SDK Method | HTTP | Endpoint | Response Type |
|-----------|------|----------|---------------|
| `GetAchievements()` | `GET` | `/achievements` | `List<Achievement>` *(paginated)* |
| `GetAchievement(slug)` | `GET` | `/achievements/{slug}` | `Achievement` |

### Slots & Equipment

| SDK Method | HTTP | Endpoint | Request Body | Response Type |
|-----------|------|----------|-------------|---------------|
| `GetStandardSlots()` | `GET` | `/slots/standard` | — | `List<Slot>` |
| `GetEquippedAssets()` | `GET` | `/slots/equipped?player_id={id}` | — | `List<EquippedSlotResponse>` |
| `EquipAsset(slotId, assetId)` | `POST` | `/slots/{slot_id}/equip` | `{ "player_id": "...", "asset_id": "..." }` | `EquippedSlotResponse` |
| `UnequipAsset(slotId)` | `POST` | `/slots/{slot_id}/unequip` | `{ "player_id": "..." }` | *(empty)* |
| `GetEquippedAsset(slotName)` | `GET` | `/slots/{slot_name}/equipped/player?player_id={id}` | — | `EquippedSlotResponse` |

### Assets

| SDK Method | HTTP | Endpoint | Response Type |
|-----------|------|----------|---------------|
| `GetAsset(assetId)` | `GET` | `/assets/{id}` | `Asset` |

### Authority

| SDK Method | HTTP | Endpoint | Response Type |
|-----------|------|----------|---------------|
| `GetAuthority()` | `GET` | `/authority` | `Authority` |
| `DeleteTestData()` | `DELETE` | `/authority/test-data` | *(empty)* |

### Pagination

List endpoints use **cursor-based pagination**:
```
GET /v1/achievements?limit=25&cursor=last-item-id
```

Response envelope:
```json
{
    "data": [ ... ],
    "next_cursor": "some-uuid-or-null"
}
```

- `limit`: max items per page (default 50, max 200)
- `cursor`: the `next_cursor` from previous response
- `next_cursor: null` = last page
- **The SDK auto-paginates all list operations** — game devs never deal with cursors.

### Headers

| Header | When | Example |
|--------|------|---------|
| `Authorization` | All requests (except `GET /v1/slots/standard`) | `Bearer ovn_test_abc123` |
| `Content-Type` | POST/PUT requests | `application/json` |
| `Idempotency-Key` | `IssueAchievement` (optional, auto-generated for offline queue) | `a1b2c3d4e5f6` |

---

## 16. Data Models Reference

All models live in the `Ovation.Models` namespace. JSON deserialization uses Newtonsoft.Json with `[JsonProperty]` attributes.

### `Player`

```json
{
    "id": "e2f3a4b5-c6d7-8901-ef23-456789abcdef",
    "anonymous": true,
    "external_id": "steam_76561198012345",
    "achievements": [ /* PlayerAchievement[] */ ],
    "created_at": "2026-03-10T12:00:00+00:00"
}
```

| C# Property | JSON Key | Type | Notes |
|-------------|----------|------|-------|
| `Id` | `id` | `string` | UUID |
| `Anonymous` | `anonymous` | `bool` | True until linked to Ovation account |
| `ExternalId` | `external_id` | `string` | Nullable, per-authority |
| `Achievements` | `achievements` | `List<PlayerAchievement>` | May be empty |
| `CreatedAt` | `created_at` | `DateTimeOffset` | ISO 8601 |

### `Achievement`

```json
{
    "id": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
    "slug": "first-blood",
    "display_name": "First Blood",
    "description": "Defeat your first enemy in combat",
    "repeatable": false,
    "archived": false,
    "is_hidden": false,
    "rarity_percentage": 45.2,
    "slot_assets": { "slot-uuid": "asset-uuid" },
    "created_at": "2026-03-10T12:00:00+00:00",
    "updated_at": "2026-03-10T12:00:00+00:00",
    "test_mode": false
}
```

| C# Property | JSON Key | Type | Notes |
|-------------|----------|------|-------|
| `Id` | `id` | `string` | UUID |
| `Slug` | `slug` | `string` | Immutable identifier |
| `DisplayName` | `display_name` | `string` | Human-readable |
| `Description` | `description` | `string` | Nullable |
| `Repeatable` | `repeatable` | `bool` | Can be earned multiple times |
| `Archived` | `archived` | `bool` | Soft-deleted |
| `IsHidden` | `is_hidden` | `bool` | Hidden until earned |
| `RarityPercentage` | `rarity_percentage` | `float?` | 0-100, nullable |
| `SlotAssets` | `slot_assets` | `Dictionary<string, string>` | slot_id → asset_id |
| `CreatedAt` | `created_at` | `DateTimeOffset` | |
| `UpdatedAt` | `updated_at` | `DateTimeOffset` | |
| `TestMode` | `test_mode` | `bool` | Created with test key |

### `PlayerAchievement`

```json
{
    "slug": "first-blood",
    "display_name": "First Blood",
    "description": "Defeat your first enemy in combat",
    "authority_id": "uuid",
    "authority_name": "Cool Game Studio",
    "earned_at": "2026-03-10T12:00:00+00:00",
    "assets": [ /* AssetSummary[] */ ]
}
```

| C# Property | JSON Key | Type |
|-------------|----------|------|
| `Slug` | `slug` | `string` |
| `DisplayName` | `display_name` | `string` |
| `Description` | `description` | `string` |
| `AuthorityId` | `authority_id` | `string` |
| `AuthorityName` | `authority_name` | `string` |
| `EarnedAt` | `earned_at` | `DateTimeOffset` |
| `Assets` | `assets` | `List<AssetSummary>` |

### `AssetSummary`

```json
{
    "id": "asset-uuid",
    "slot_id": "slot-uuid",
    "slot_name": "badge",
    "url": "https://...",
    "version": 1,
    "display_name": "Gold Star Badge"
}
```

| C# Property | JSON Key | Type |
|-------------|----------|------|
| `Id` | `id` | `string` |
| `SlotId` | `slot_id` | `string` |
| `SlotName` | `slot_name` | `string` |
| `Url` | `url` | `string` |
| `Version` | `version` | `int` |
| `DisplayName` | `display_name` | `string` |

### `Asset`

```json
{
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "asset_type": "image",
    "slot_id": "slot-uuid",
    "slot_name": "avatar_frame",
    "display_name": "Gold Star Badge",
    "authority_attribution": "Art by Jane Doe",
    "current_version": 1,
    "url": "https://ovation-assets-dev.s3.amazonaws.com/assets/abc123/v1/badge.png",
    "text_content": null,
    "created_at": "2026-03-10T12:00:00+00:00",
    "updated_at": "2026-03-10T12:00:00+00:00"
}
```

| C# Property | JSON Key | Type | Notes |
|-------------|----------|------|-------|
| `Id` | `id` | `string` | |
| `AssetType` | `asset_type` | `string` | "image", "text", "audio" |
| `SlotId` | `slot_id` | `string` | |
| `SlotName` | `slot_name` | `string` | |
| `DisplayName` | `display_name` | `string` | |
| `AuthorityAttribution` | `authority_attribution` | `string` | Nullable |
| `CurrentVersion` | `current_version` | `int` | |
| `Url` | `url` | `string` | Null for text assets |
| `TextContent` | `text_content` | `string` | Null for image assets |
| `CreatedAt` | `created_at` | `DateTimeOffset` | |
| `UpdatedAt` | `updated_at` | `DateTimeOffset` | |

### `Slot`

```json
{
    "id": "c3d4e5f6-a7b8-9012-cdef-345678901234",
    "name": "avatar_frame",
    "display_name": "Avatar Frame",
    "description": "Decorative border around a player's avatar",
    "asset_type": "image",
    "file_formats": ["png", "webp"],
    "width": 256,
    "height": 256,
    "inner_width": 192,
    "inner_height": 192,
    "max_file_size_bytes": 131072,
    "transparency": "required",
    "animation_allowed": false,
    "text_max_length": null,
    "text_allowed_pattern": null,
    "authority_guidance": "Create a 256x256 frame...",
    "implementation_notes": "Overlay on top of avatar...",
    "created_at": "2026-03-10T12:00:00+00:00",
    "updated_at": "2026-03-10T12:00:00+00:00"
}
```

| C# Property | JSON Key | Type | Notes |
|-------------|----------|------|-------|
| `Id` | `id` | `string` | |
| `Name` | `name` | `string` | e.g., "badge", "avatar_frame" |
| `DisplayName` | `display_name` | `string` | |
| `Description` | `description` | `string` | |
| `AssetType` | `asset_type` | `string` | "image", "text", "audio" |
| `FileFormats` | `file_formats` | `List<string>` | Nullable |
| `Width` | `width` | `int?` | |
| `Height` | `height` | `int?` | |
| `InnerWidth` | `inner_width` | `int?` | For compositing slots |
| `InnerHeight` | `inner_height` | `int?` | |
| `MaxFileSizeBytes` | `max_file_size_bytes` | `int?` | |
| `Transparency` | `transparency` | `string` | "required", "optional", "forbidden" |
| `AnimationAllowed` | `animation_allowed` | `bool` | |
| `TextMaxLength` | `text_max_length` | `int?` | |
| `TextAllowedPattern` | `text_allowed_pattern` | `string` | Regex |
| `AuthorityGuidance` | `authority_guidance` | `string` | |
| `ImplementationNotes` | `implementation_notes` | `string` | |

### `EquippedAsset` / `EquippedSlotResponse`

```json
{
    "slot": "avatar_frame",
    "player_id": "player-uuid",
    "equipped_asset": {
        "id": "asset-uuid",
        "asset_type": "image",
        "url": "https://...",
        "version": 1,
        "display_name": "Gold Star Badge",
        "achievement_slug": "first-blood",
        "authority_name": "Cool Game Studio"
    }
}
```

### `Authority`

```json
{
    "id": "4d38f902-de2e-487d-8dbd-f4452fc2b4a1",
    "name": "Cool Game Studio",
    "type": "game_studio",
    "website": "https://coolgame.com",
    "verified": true,
    "created_at": "2026-03-10T12:00:00+00:00"
}
```

| C# Property | JSON Key | Type | Notes |
|-------------|----------|------|-------|
| `Id` | `id` | `string` | |
| `Name` | `name` | `string` | |
| `Type` | `type` | `string` | "game_studio", "brand", "business", "platform", "event", "web_app" |
| `Website` | `website` | `string` | Nullable |
| `Verified` | `verified` | `bool` | |
| `CreatedAt` | `created_at` | `DateTimeOffset` | |

### `IssueAchievementResult`

See [Section 7](#7-achievement-operations).

### `ExternalIdResponse`

See [Section 6](#6-player-identity-system).

### `PaginatedResponse<T>`

Internal wrapper for cursor-based pagination:

```json
{
    "data": [ /* T[] */ ],
    "next_cursor": "uuid-or-null"
}
```

### `OvationError` / `ErrorResponse`

See [Section 12](#12-error-handling).

---

## 17. Editor Tools

### Setup Wizard (`Ovation > Setup Wizard`)

Step-by-step wizard for first-time setup:
1. **Welcome** — explains what's needed
2. **API Key** — enter key, validates format (detects test vs live)
3. **Environment** — select Test/Live, optional base URL override
4. **Create Config** — creates `OvationConfig` ScriptableObject with entered settings

### Config Inspector

Custom inspector for `OvationConfig` assets:
- Masked API key field (show/hide toggle)
- Security warning about not embedding keys in builds
- Runtime info panel (in Play Mode): player ID, initialized state, queue count

### Dev Tools (`Ovation > Dev Tools`)

Runtime testing panel (Play Mode only):
- **SDK Status:** initialized, player ID, offline queue count
- **Issue Achievement:** type a slug and click "Unlock", or use quick-fire buttons for loaded achievements
- **Achievement Browser:** fetch and display all authority achievements with slug, name, rarity, status
- **Player Achievements:** fetch and display earned achievements with dates and bound assets
- **Player Tools:** link external IDs, copy player ID to clipboard
- **Danger Zone:** reset player ID (clears PlayerPrefs), delete all test data (calls `DELETE /v1/authority/test-data`)

---

## 18. Architecture & Internals

### Package Structure

```
ovation-unity/
├── package.json                        # UPM manifest (games.ovation.sdk)
├── Runtime/
│   ├── Ovation.Runtime.asmdef          # Assembly definition
│   ├── OvationSDK.cs                   # Main singleton (MonoBehaviour) — public API surface
│   ├── OvationConfig.cs                # ScriptableObject configuration
│   ├── Ovation.cs                      # Placeholder (reserved)
│   ├── link.xml                        # IL2CPP code stripping prevention
│   ├── Api/
│   │   ├── OvationApiClient.cs         # Core HTTP layer (UnityWebRequest)
│   │   ├── PlayerService.cs            # Player API operations
│   │   ├── AchievementService.cs       # Achievement API operations
│   │   ├── SlotService.cs              # Slot/equipment API operations
│   │   └── AssetService.cs             # Asset fetching (read-only)
│   ├── Models/                         # C# data models (12 files)
│   ├── Cache/
│   │   └── AssetCache.cs               # Image download + LRU disk cache
│   ├── Queue/
│   │   ├── OfflineQueue.cs             # Offline request queueing + retry
│   │   └── QueuedRequest.cs            # Serializable queued request
│   ├── Identity/
│   │   └── PlayerIdentityManager.cs    # Auto player ID creation + PlayerPrefs persistence
│   ├── UI/
│   │   ├── AchievementToast.cs         # Toast notification controller
│   │   ├── AchievementToastConfig.cs   # Toast customization ScriptableObject
│   │   └── EmbeddedIcon.cs             # Base64 Ovation icon (no asset dependency)
│   └── Utils/
│       ├── OvationLogger.cs            # [Ovation] prefixed logging
│       └── IdempotencyKeyGenerator.cs  # GUID-based idempotency keys
├── Editor/
│   ├── Ovation.Editor.asmdef
│   ├── OvationConfigEditor.cs          # Custom config inspector
│   ├── OvationSetupWizard.cs           # First-time setup wizard
│   └── OvationDevTools.cs              # Runtime testing panel
├── Tests/
│   ├── Editor/                         # Config validation tests
│   └── Runtime/                        # Model deserialization tests
└── Samples~/
    └── BasicIntegration/
        └── BasicIntegrationSample.cs   # Complete integration example
```

### Key Design Patterns

1. **Singleton + MonoBehaviour:** `OvationSDK` inherits MonoBehaviour for Unity lifecycle. Uses `DontDestroyOnLoad`. Can be created programmatically (via `Init()`) or placed in scene.

2. **Service Layer:** Each API domain has its own internal service class:
   - `PlayerService` — create player, set external ID, issue achievement, get player achievements
   - `AchievementService` — list achievements, get by slug
   - `SlotService` — standard slots, equip/unequip, query equipped
   - `AssetService` — get asset details

3. **Dual Async API:** Every public method has:
   - Callback: `Method(args, Action<T> onSuccess, Action<OvationError> onError)`
   - Async: `async Task<T> MethodAsync(args)` — throws `OvationException` on error

4. **UnityWebRequest HTTP Client:** `OvationApiClient` handles:
   - URL construction (`{baseUrl}/v1{path}`)
   - Bearer token injection
   - JSON serialization/deserialization (Newtonsoft)
   - Error response parsing into `OvationError`
   - Auto-pagination via `GetAllPagesAsync<T>`
   - Custom header support (Idempotency-Key)
   - `await Task.Yield()` for async bridging (stays on main thread)

5. **ApiResult<T>:** Internal result wrapper — all service methods return `ApiResult<T>` which contains either `Success + Data` or `Error`. The `OvationSDK` facade translates these into callbacks/exceptions.

6. **Procedural UI:** Toast UI is built entirely in code (no prefabs/assets needed). Uses 9-sliced rounded rectangle sprites generated at runtime.

### Threading Model

Unity is single-threaded. All `UnityWebRequest` calls run on the main thread via `await Task.Yield()`. **Never use `Task.Run()` or background threads for API calls.**

### Logging

All SDK logs prefixed with `[Ovation]`:
- `Log` and `Warning` gated behind `enableDebugLogging`
- `Error` always logs regardless
- Examples:
  ```
  [Ovation] Player created: e2f3a4b5-...
  [Ovation] Achievement issued: first-blood (was_new: true)
  [Ovation] ERROR: API call failed (401): authentication_failed
  ```

---

## 19. Code Snippet Cookbook

These are the patterns game developers will use. Useful for writing portal helper snippets and documentation.

### Minimal Integration (2 lines)

```csharp
await OvationSDK.Init("your-api-key");
OvationSDK.Unlock("first-blood");
```

### Full Integration with Toast

```csharp
using Ovation;
using Ovation.UI;

async void Start()
{
    await OvationSDK.Init(apiKey, enableDebugLogging: true);
    AchievementToast.Create();
    Debug.Log($"Ready! Player: {OvationSDK.Instance.PlayerId}");
}
```

### Issue Achievement with Result Handling

```csharp
try
{
    var result = await OvationSDK.UnlockAsync("first-blood");
    if (result.WasQueued)
        Debug.Log("Offline — queued for sync");
    else if (result.WasNew)
        Debug.Log($"NEW: {result.DisplayName}!");
    else
        Debug.Log($"Already earned: {result.DisplayName}");
}
catch (OvationException ex)
{
    Debug.LogError($"Failed: {ex.Error.Code} - {ex.Error.Message}");
}
```

### Issue Achievement with Callbacks

```csharp
OvationSDK.Instance.IssueAchievement("first-blood",
    result => {
        if (result.WasNew)
            Debug.Log($"Unlocked: {result.DisplayName}!");
    },
    error => Debug.LogError($"Failed: {error.Message}")
);
```

### Browse All Achievements

```csharp
var achievements = await OvationSDK.Instance.GetAchievementsAsync();
foreach (var a in achievements)
    Debug.Log($"{a.Slug}: {a.DisplayName} ({a.RarityPercentage}%)");
```

### Show Player's Earned Achievements

```csharp
var earned = await OvationSDK.Instance.GetPlayerAchievementsAsync();
foreach (var a in earned)
{
    Debug.Log($"{a.DisplayName} from {a.AuthorityName}, earned {a.EarnedAt}");
    foreach (var asset in a.Assets)
        Debug.Log($"  [{asset.SlotName}] {asset.DisplayName}");
}
```

### Link Platform ID

```csharp
var result = await OvationSDK.Instance.SetExternalIdAsync("steam_76561198012345");
Debug.Log($"Linked: {result.ExternalId}");
```

### Load and Display Achievement Badge

```csharp
var achievement = await OvationSDK.Instance.GetAchievementAsync("first-blood");
if (achievement.SlotAssets != null && achievement.SlotAssets.Count > 0)
{
    var assetId = achievement.SlotAssets.Values.First();
    var asset = await OvationSDK.Instance.GetAssetAsync(assetId);
    var sprite = await OvationSDK.Instance.LoadAssetSpriteAsync(asset.Url, asset.CurrentVersion);
    myImage.sprite = sprite;
}
```

### Equip/Unequip Assets

```csharp
// Equip
var result = await OvationSDK.Instance.EquipAssetAsync(slotId, assetId);

// Check what's equipped
var equipped = await OvationSDK.Instance.GetEquippedAssetAsync("avatar_frame");
if (equipped.EquippedAsset != null)
    Debug.Log($"Wearing: {equipped.EquippedAsset.DisplayName}");

// Unequip
await OvationSDK.Instance.UnequipAssetAsync(slotId);
```

### Listen for Offline Queue Sync

```csharp
OvationSDK.Instance.OnQueuedAchievementSynced += (slug, result) => {
    ShowNotification($"Previously queued '{slug}' has been synced!");
};
```

### Custom Toast Configuration

```csharp
var config = ScriptableObject.CreateInstance<AchievementToastConfig>();
config.position = ToastPosition.TopCenter;
config.displayDuration = 5f;
config.backgroundColor = new Color(0.1f, 0.1f, 0.2f, 0.95f);
config.accentColor = new Color(1f, 0.84f, 0f); // Gold
AchievementToast.Create(config);
```

### Get Authority Info

```csharp
var authority = await OvationSDK.Instance.GetAuthorityAsync();
Debug.Log($"Authority: {authority.Name} ({authority.Type}), verified: {authority.Verified}");
```

### Delete Test Data (Test Keys Only)

```csharp
OvationSDK.Instance.DeleteTestData(
    () => Debug.Log("All test data wiped"),
    error => Debug.LogError($"Failed: {error.Message}")
);
```

---

## Appendix: What the SDK Does NOT Do

These are authority/portal/server-side operations — **not** in the SDK:

- Create, update, or delete achievements (portal feature)
- Upload assets (portal feature)
- Manage webhooks (server-side)
- User registration/login (portal feature)
- Authority management (portal/admin)
- Auto-detect editor vs build for environment switching
