# Ovation Unity SDK — Master Context & Specification

This document contains everything needed to build the Ovation Unity SDK. It includes business context, full API documentation, exact request/response schemas, and detailed SDK requirements.

---

## Table of Contents

1. [What is Ovation?](#what-is-ovation)
2. [Core Concepts](#core-concepts)
3. [API Reference](#api-reference)
4. [Exact Request/Response Schemas](#exact-requestresponse-schemas)
5. [SDK Requirements & Specification](#sdk-requirements--specification)
6. [SDK Architecture](#sdk-architecture)
7. [Detailed Feature Specifications](#detailed-feature-specifications)

---

## What is Ovation?

Ovation is a **universal achievement platform**. It's a centralized system for issuing, tracking, and verifying achievements across games, apps, and platforms. Think of it as a cross-platform equivalent to Xbox Achievements or Steam Achievements that any game studio, brand, or organization can integrate with.

**Core value proposition:**
- Game studios (called "Authorities") register and define achievements
- Games issue achievements to players via API
- Players accumulate achievements across multiple games into unified profiles
- Assets (badges, icons, sprays, emojis) are versioned and standardized across slots
- Real-time webhook notifications keep authorities informed of events

**API Base URL (Dev):** `https://dev.api.ovation.games`

---

## Core Concepts

### Authority
An **Authority** is any organization that issues achievements — a game studio, brand, platform, or event organizer. It's the top-level tenant. Everything (achievements, assets, players, webhooks) is scoped to an authority. Games authenticate as their authority via API keys.

### Achievement
An **Achievement** is something a player can earn. It has:
- A **slug** (immutable identifier like `first-blood`) — this is what game code references
- A **display name** (human-readable, can be updated)
- Optional **assets** (visual representations like badges)
- A **repeatable** flag — can the same player earn it multiple times?
- An **is_hidden** flag — hidden until unlocked
- A **rarity_percentage** — cached calculation of how rare the achievement is

Achievements use **soft delete** (`archived` flag). Archived achievements can't be issued but remain in player histories.

### Player
A **Player** starts as anonymous — just a UUID. The game creates a player on first launch and gets back an ID it stores locally. Players can have different **external IDs** per authority (Steam ID for one game, Xbox gamertag for another).

Players exist globally (single UUID across all authorities) — this is the foundation for cross-game profiles.

### Asset
An **Asset** is a file (image) or text content that belongs to a specific slot. Assets are:
- **Versioned** — uploading a new file creates a new version; old ones are preserved
- **Slot-bound** — each asset belongs to exactly one slot, which determines its type and validation rules
- **Bound to achievements** — achievements can reference multiple assets via slot_assets

### Slot
A **Slot** is a global customization point defined by Ovation (not per-authority). It defines "where can a player display something?" Authorities opt in to which slots they support.

**Standard Phase 1 slots:**

| Slot | Asset Type | Dimensions | Max Size | Key Details |
|------|-----------|-----------|----------|-------------|
| `badge` | image | 512x512 | 256KB | Transparent background required |
| `nameplate_title` | text | — | — | Max 24 chars, alphanumeric + spaces/hyphens/periods |
| `profile_banner` | image | 1200x400 | 512KB | No transparency (background fill) |
| `avatar_frame` | image | 256x256 (inner 192x192) | 128KB | Transparent center for avatar compositing |
| `player_icon` | image | 256x256 | 128KB | Transparency optional |
| `emoji` | image | 128x128 | 64KB | Transparent background required |
| `spray` | image | 512x512 | 256KB | Transparency optional |

---

## API Reference

### Authentication

The SDK uses **API Key Authentication**:

```
Authorization: Bearer ovn_live_aB3cD4eF5gH6...
```

- Key format: `ovn_{environment}_{random_token}`
- Environment is `live` or `test`
- Keys are tied to an Authority — all operations scoped to that authority
- Test keys (`ovn_test_*`) create test-mode data, completely isolated from live data

### Base URL

All endpoints are prefixed with `/v1`. The SDK should allow configuring the base URL (default: `https://api.ovation.games`). 

### Error Format

All errors return:
```json
{
  "error": {
    "code": "machine_readable_error_code",
    "message": "Human-readable error message."
  }
}
```

**Error codes the SDK must handle:**

| HTTP Status | Code | Meaning |
|-------------|------|---------|
| 400 | `invalid_request` | Bad request body or parameters |
| 401 | `authentication_failed` | Missing, invalid, or inactive API key |
| 404 | `achievement_not_found` | Achievement slug doesn't exist for this authority |
| 404 | `player_not_found` | Player ID doesn't exist |
| 404 | `asset_not_found` | Asset ID doesn't exist for this authority |
| 404 | `slot_not_found` | Slot doesn't exist |
| 409 | `slug_already_exists` | Achievement slug already taken in this authority |
| 409 | `external_id_conflict` | External ID already linked to a different player |
| 410 | `achievement_archived` | Trying to issue an archived achievement |

### Pagination

List endpoints use **cursor-based pagination**:

```
GET /v1/achievements?limit=25&cursor=last-item-id
```

Response:
```json
{
  "data": [ ... ],
  "next_cursor": "some-uuid-or-null"
}
```

- `limit`: max items per page (default 50, max 200)
- `cursor`: the `next_cursor` value from the previous response
- When `next_cursor` is `null`, you've reached the last page

### SDK-Relevant Endpoints

The following endpoints are in scope for the Unity SDK (game-client operations only):

#### Players

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/v1/players` | Create anonymous player (no body needed) |
| `GET` | `/v1/players/{id}` | Get player with achievements |
| `GET` | `/v1/players/by-external-id/{external_id}` | Look up player by external ID |
| `POST` | `/v1/players/{id}/achievements` | Issue achievement to player |
| `GET` | `/v1/players/{id}/achievements` | List player's achievements (paginated) |
| `PUT` | `/v1/players/{id}/external-id` | Set or update external ID mapping |

#### Achievements

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/v1/achievements` | List achievements (paginated) |
| `GET` | `/v1/achievements/{slug}` | Get one achievement by slug |

#### Slots & Equipment

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/v1/slots/standard` | List all standard slots with specs (no auth required) |
| `GET` | `/v1/slots/equipped` | Get all equipped assets for a player (query: `player_id`) |
| `POST` | `/v1/slots/{slot_id}/equip` | Equip an asset in a slot |
| `POST` | `/v1/slots/{slot_id}/unequip` | Remove equipped asset from a slot |
| `GET` | `/v1/slots/{slot_name}/equipped/player` | Get what a specific player has equipped in a slot |

#### Assets (read-only for SDK)

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/v1/assets/{id}` | Get asset details with current version URL |

#### Authority (read-only for SDK)

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/v1/authority` | Get your authority's profile |
| `DELETE` | `/v1/authority/test-data` | Wipe all test data (test key only) |

---

## Exact Request/Response Schemas

### Common Envelope

**Paginated Response:**
```csharp
{
    "data": [T, ...],          // Array of results
    "next_cursor": "uuid"|null // Null = last page
}
```

**Error Response:**
```csharp
{
    "error": {
        "code": "string",     // Machine-readable
        "message": "string"   // Human-readable
    }
}
```

### Player Schemas

**POST /v1/players** — Create player
- Request: No body
- Response (201):
```json
{
    "id": "e2f3a4b5-c6d7-8901-ef23-456789abcdef",
    "anonymous": true,
    "external_id": null,
    "achievements": [],
    "created_at": "2026-03-10T12:00:00+00:00"
}
```

**GET /v1/players/{id}** — Get player
- Response (200):
```json
{
    "id": "e2f3a4b5-c6d7-8901-ef23-456789abcdef",
    "anonymous": true,
    "external_id": "steam_76561198012345",
    "achievements": [
        {
            "slug": "first-blood",
            "display_name": "First Blood",
            "description": "Defeat your first enemy in combat",
            "authority_id": "4d38f902-de2e-487d-8dbd-f4452fc2b4a1",
            "authority_name": "Cool Game Studio",
            "earned_at": "2026-03-10T12:00:00+00:00",
            "assets": [
                {
                    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
                    "slot_id": "slot-uuid",
                    "slot_name": "badge",
                    "url": "https://ovation-assets-dev.s3.amazonaws.com/assets/badge.png",
                    "version": 1,
                    "display_name": "Gold Star Badge"
                }
            ]
        }
    ],
    "created_at": "2026-03-10T12:00:00+00:00"
}
```

**PUT /v1/players/{id}/external-id** — Set external ID
- Request:
```json
{
    "external_id": "steam_76561198012345"
}
```
- Response (200):
```json
{
    "player_id": "e2f3a4b5-c6d7-8901-ef23-456789abcdef",
    "authority_id": "4d38f902-de2e-487d-8dbd-f4452fc2b4a1",
    "external_id": "steam_76561198012345"
}
```

### Achievement Schemas

**POST /v1/players/{id}/achievements** — Issue achievement
- Request:
```json
{
    "slug": "first-blood"
}
```
- Optional Header: `Idempotency-Key: unique-request-id`
- Response (201 if new, 200 if already earned):
```json
{
    "slug": "first-blood",
    "display_name": "First Blood",
    "earned_at": "2026-03-10T12:00:00+00:00",
    "was_new": true
}
```

**GET /v1/achievements** — List achievements (paginated)
- Query params: `limit` (default 50, max 200), `cursor`
- Response (200):
```json
{
    "data": [
        {
            "id": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
            "slug": "first-blood",
            "display_name": "First Blood",
            "description": "Defeat your first enemy in combat",
            "repeatable": false,
            "archived": false,
            "is_hidden": false,
            "rarity_percentage": 45.2,
            "slot_assets": {"slot-id": "asset-id"},
            "created_at": "2026-03-10T12:00:00+00:00",
            "updated_at": "2026-03-10T12:00:00+00:00",
            "test_mode": false
        }
    ],
    "next_cursor": "uuid-or-null"
}
```

**GET /v1/achievements/{slug}** — Get single achievement
- Response (200): Same shape as single item in list above

**GET /v1/players/{id}/achievements** — List player's achievements (paginated)
- Query params: `limit`, `cursor`, optional `authority_id` filter
- Response (200):
```json
{
    "data": [
        {
            "slug": "first-blood",
            "display_name": "First Blood",
            "description": "Defeat your first enemy in combat",
            "authority_id": "uuid",
            "authority_name": "Cool Game Studio",
            "earned_at": "2026-03-10T12:00:00+00:00",
            "assets": [
                {
                    "id": "asset-uuid",
                    "slot_id": "slot-uuid",
                    "slot_name": "badge",
                    "url": "https://...",
                    "version": 1,
                    "display_name": "Gold Star Badge"
                }
            ]
        }
    ],
    "next_cursor": "uuid-or-null"
}
```

### Slot & Equipment Schemas

**GET /v1/slots/standard** — List standard slots
- No auth required
- Response (200):
```json
[
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
        "authority_guidance": "Create a 256x256 frame with transparent 192x192 center...",
        "implementation_notes": "Overlay this frame on top of the player's avatar...",
        "created_at": "2026-03-10T12:00:00+00:00",
        "updated_at": "2026-03-10T12:00:00+00:00"
    }
]
```

**GET /v1/slots/equipped?player_id={id}** — Get all equipped assets
- Response (200): Array of slot-equipped pairs

**POST /v1/slots/{slot_id}/equip** — Equip asset
- Request:
```json
{
    "player_id": "player-uuid",
    "asset_id": "asset-uuid"
}
```
- Response (200): Equipped asset details

**POST /v1/slots/{slot_id}/unequip** — Unequip asset
- Request:
```json
{
    "player_id": "player-uuid"
}
```

**GET /v1/slots/{slot_name}/equipped/player?player_id={id}** — Get equipped asset for player in slot
- Response (200):
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

### Asset Schema

**GET /v1/assets/{id}** — Get asset details
- Response (200):
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

### Authority Schema

**GET /v1/authority** — Get authority profile
- Response (200):
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

---

## SDK Requirements & Specification

### Target Platform
- **Unity 2022.3 LTS** (minimum version, the only version to target)
- **C# language** (Unity's scripting language)
- Distributed as a **Unity Package Manager (UPM)** package via Git URL

### Core Requirements

1. **HTTP Layer** — Use `UnityWebRequest` (NOT `System.Net.Http`). UnityWebRequest works across all Unity platforms (desktop, mobile, console, WebGL).

2. **Async Patterns** — Support both:
   - Coroutines (traditional Unity pattern)
   - async/await (modern C#, works in Unity 2022.3+)

3. **Configuration via ScriptableObject** — A `OvationConfig` asset that developers drag into their project with:
   - API key (string field)
   - Environment toggle (test/live) — manual, not auto-detected
   - Base URL override (optional, defaults to `https://api.ovation.games`)
   - Enable/disable debug logging

4. **Singleton Client** — `OvationSDK.Instance` that persists across scenes via `DontDestroyOnLoad`. Initialized from the config ScriptableObject.

5. **JSON Serialization** — Use Newtonsoft Json.NET via Unity's official package (`com.unity.nuget.newtonsoft-json`). Unity's built-in `JsonUtility` is too limited for this.

6. **Strong C# Models** — Every API response shape gets a C# class with proper types (no raw JSON/dictionaries in the public API).

7. **Callbacks + Events** — Fire C# events when key things happen:
   - `OnAchievementEarned(AchievementEarnedEvent)` — when an achievement is successfully issued
   - `OnError(OvationError)` — when an API call fails

### Feature Requirements

#### F1: Automatic Player Identity Management
The SDK automatically handles player identity:
- On first use, call `POST /v1/players` to create an anonymous player
- Store the player UUID in `PlayerPrefs` (Unity's local key-value storage)
- On subsequent sessions, reuse the stored UUID
- Provide `OvationSDK.Instance.PlayerId` to access the current player's UUID
- Provide an opt-out for games that want to manage player IDs themselves
- Provide `OvationSDK.Instance.SetExternalId(string externalId)` to link platform IDs

#### F2: Achievement Operations
- `GetAchievements(Action<List<Achievement>> onSuccess, Action<OvationError> onError)` — list all achievements (auto-paginate)
- `GetAchievement(string slug, Action<Achievement> onSuccess, Action<OvationError> onError)` — get single achievement
- `IssueAchievement(string slug, Action<IssueAchievementResult> onSuccess, Action<OvationError> onError, string idempotencyKey = null)` — issue to current player
- `GetPlayerAchievements(Action<List<PlayerAchievement>> onSuccess, Action<OvationError> onError)` — get current player's achievements (auto-paginate)
- All methods should also have async/await variants returning `Task<T>` or `Awaitable<T>`

#### F3: Slot & Equipment Operations
- `GetStandardSlots(...)` — list all standard slots with specs
- `GetEquippedAssets(...)` — get all equipped assets for current player
- `EquipAsset(string slotId, string assetId, ...)` — equip an asset
- `UnequipAsset(string slotId, ...)` — unequip an asset
- `GetEquippedAsset(string slotName, ...)` — get what's equipped in a specific slot

#### F4: Asset Download & Caching
The SDK should download and cache asset images:
- Download images from asset URLs
- Cache to `Application.persistentDataPath` (survives app restarts)
- Load cached images as Unity `Texture2D` or `Sprite` objects
- Provide: `LoadAssetImage(string url, Action<Texture2D> onLoaded)` and `LoadAssetSprite(string url, Action<Sprite> onLoaded)`
- Cache invalidation based on asset version (re-download when version changes)
- Max cache size configuration with LRU eviction

#### F5: Achievement Notification UI (Toast)
Include an optional, built-in achievement popup:
- A default prefab with clean, modern design (similar to Xbox/Steam achievement popups)
- Shows achievement name, description, and badge image
- Slides in, stays for ~3 seconds, slides out
- Customizable: colors, font, position, animation, duration
- Easy to disable for games that want their own UI
- Fires automatically when `IssueAchievement` returns `was_new: true`
- Make the prefab and toast system completely optional — the SDK works fine without it

#### F6: Offline Queue
When API calls fail due to connectivity:
- Queue failed `IssueAchievement` requests locally (serialize to disk)
- On next successful API call (or periodic check), retry queued requests
- Use idempotency keys to prevent duplicates on retry
- Queue is persisted to `Application.persistentDataPath` (survives app restarts)
- Fire events when queued items are successfully synced
- Max queue size to prevent unbounded storage

### Non-Requirements (NOT in scope for V1)
- Authority management (portal/admin feature)
- Webhook management (server-side only)
- Asset uploading (portal feature)
- User registration/login (portal feature)
- Achievement creation/update/delete (portal feature)
- Auto-detection of editor vs build for environment switching

---

## SDK Architecture

### Package Structure

```
ovation-unity-sdk/
├── package.json                    # UPM package manifest
├── README.md                       # SDK documentation for game developers
├── CHANGELOG.md                    # Version history
├── LICENSE                         # License file
├── Runtime/
│   ├── Ovation.Runtime.asmdef      # Assembly definition
│   ├── OvationSDK.cs               # Main singleton entry point
│   ├── OvationConfig.cs            # ScriptableObject configuration
│   ├── Api/
│   │   ├── OvationApiClient.cs     # Core HTTP layer (UnityWebRequest)
│   │   ├── PlayerService.cs        # Player API operations
│   │   ├── AchievementService.cs   # Achievement API operations
│   │   ├── SlotService.cs          # Slot/equipment API operations
│   │   └── AssetService.cs         # Asset fetching (read-only)
│   ├── Models/
│   │   ├── Player.cs               # Player data model
│   │   ├── Achievement.cs          # Achievement data model
│   │   ├── IssueAchievementResult.cs
│   │   ├── PlayerAchievement.cs    # Player's earned achievement
│   │   ├── Asset.cs                # Asset data model
│   │   ├── AssetSummary.cs         # Compact asset info
│   │   ├── Slot.cs                 # Slot data model
│   │   ├── EquippedAsset.cs        # Equipped asset data model
│   │   ├── Authority.cs            # Authority data model
│   │   ├── OvationError.cs         # Error model
│   │   └── PaginatedResponse.cs    # Generic paginated response
│   ├── Cache/
│   │   ├── AssetCache.cs           # Image download + disk cache
│   │   └── CacheConfig.cs         # Cache size limits, TTL
│   ├── Queue/
│   │   ├── OfflineQueue.cs         # Offline request queueing
│   │   └── QueuedRequest.cs        # Serializable queued request
│   ├── Identity/
│   │   └── PlayerIdentityManager.cs # Auto player ID creation + persistence
│   └── Utils/
│       ├── OvationLogger.cs        # Conditional debug logging
│       └── IdempotencyKeyGenerator.cs
├── Runtime/UI/
│   ├── AchievementToast.cs         # Toast notification controller
│   ├── AchievementToastConfig.cs   # Toast customization ScriptableObject
│   └── Prefabs/
│       └── OvationAchievementToast.prefab
├── Editor/
│   ├── Ovation.Editor.asmdef       # Editor assembly definition
│   ├── OvationConfigEditor.cs      # Custom inspector for OvationConfig
│   └── OvationSetupWizard.cs       # First-time setup helper window
├── Samples~/                       # Optional samples (excluded from import by default)
│   └── BasicIntegration/
│       ├── BasicIntegrationSample.cs
│       └── BasicIntegrationScene.unity
├── Tests/
│   ├── Runtime/
│   │   ├── Ovation.Tests.Runtime.asmdef
│   │   └── OvationApiClientTests.cs
│   └── Editor/
│       ├── Ovation.Tests.Editor.asmdef
│       └── OvationConfigTests.cs
└── Documentation~/
    └── ovation-unity-sdk.md        # Full docs (UPM convention)
```

### package.json

```json
{
    "name": "games.ovation.sdk",
    "version": "0.1.0",
    "displayName": "Ovation SDK",
    "description": "Universal achievement platform SDK for Unity. Issue achievements, manage player profiles, and display cosmetic assets.",
    "unity": "2022.3",
    "documentationUrl": "https://docs.ovation.games",
    "dependencies": {
        "com.unity.nuget.newtonsoft-json": "3.2.1"
    },
    "keywords": ["achievements", "gamification", "ovation", "sdk"],
    "author": {
        "name": "Ovation Games",
        "url": "https://ovation.games"
    }
}
```

### Key Design Patterns

1. **Singleton with MonoBehaviour** — `OvationSDK` inherits from `MonoBehaviour` for Unity lifecycle integration. Uses `DontDestroyOnLoad` to persist across scenes.

2. **Service Layer** — Each API domain (players, achievements, slots) has its own service class. The `OvationSDK` facade delegates to these.

3. **Dual Async API** — Every public method has both:
   - Callback version: `GetAchievements(Action<List<Achievement>> onSuccess, Action<OvationError> onError)`
   - Async version: `async Task<List<Achievement>> GetAchievementsAsync()`

4. **UnityWebRequest Wrapper** — `OvationApiClient` handles:
   - Base URL construction
   - Bearer token injection
   - JSON serialization/deserialization (Newtonsoft)
   - Error response parsing
   - Custom header support (Idempotency-Key)
   - Coroutine-to-Task bridging

5. **ScriptableObject Config** — Drag-and-drop configuration in the Unity Inspector. Created via `Assets > Create > Ovation > Config`.

### Usage Example (what game devs will write)

```csharp
using Ovation;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    void Start()
    {
        // SDK auto-initializes from OvationConfig asset
        // Player ID is auto-created and persisted

        // Issue an achievement
        OvationSDK.Instance.IssueAchievement("first-blood",
            result => {
                if (result.WasNew)
                    Debug.Log($"Achievement unlocked: {result.DisplayName}!");
            },
            error => Debug.LogError($"Failed: {error.Message}")
        );
    }

    async void CheckAchievements()
    {
        // Async variant
        var achievements = await OvationSDK.Instance.GetAchievementsAsync();
        foreach (var a in achievements)
            Debug.Log($"{a.DisplayName} - {a.RarityPercentage}% of players earned this");
    }

    void SetSteamId(string steamId)
    {
        OvationSDK.Instance.SetExternalId(steamId);
    }
}
```

---

## Detailed Feature Specifications

### Asset Cache Specification

**Cache Location:** `Application.persistentDataPath + "/OvationCache/"`

**Cache Key:** SHA256 hash of the URL + version number

**Cache Behavior:**
- First request for an asset: download from URL, save to disk, return Texture2D
- Subsequent requests: load from disk cache, return Texture2D
- When asset version changes: re-download and overwrite cached file
- Cache metadata stored in a JSON index file for fast lookups

**Configuration:**
- `maxCacheSizeMB` (default: 50MB) — oldest files evicted when exceeded
- LRU eviction — least recently accessed files deleted first

**API:**
```csharp
// Load as Texture2D
OvationSDK.Instance.LoadAssetTexture(assetUrl, version, texture => {
    renderer.material.mainTexture = texture;
});

// Load as Sprite (for UI)
OvationSDK.Instance.LoadAssetSprite(assetUrl, version, sprite => {
    image.sprite = sprite;
});
```

### Offline Queue Specification

**Queue Location:** `Application.persistentDataPath + "/OvationQueue/queue.json"`

**What gets queued:** Only `IssueAchievement` calls (the most critical game-side operation)

**Queue Behavior:**
- When an `IssueAchievement` call fails due to network error (not 4xx errors — those are real failures):
  1. Generate an idempotency key if one wasn't provided
  2. Serialize the request to the queue file
  3. Return a "queued" result to the caller
- On next successful API call, check and process the queue
- Also check queue periodically (every 60 seconds when the app has focus)
- Each queued item retried up to 5 times with exponential backoff
- Queue capped at 100 items (oldest dropped if exceeded)

**Events:**
```csharp
OvationSDK.Instance.OnQueuedAchievementSynced += (slug, result) => {
    Debug.Log($"Previously queued achievement '{slug}' synced successfully");
};
```

### Achievement Toast Specification

**Default Appearance:**
- Semi-transparent dark background panel
- Achievement badge image on the left
- Achievement name (bold) and description on the right
- Slides in from top-right corner
- Stays visible for 3 seconds
- Slides out

**Setup:**
1. Developer adds `OvationAchievementToast` prefab to their scene (or it auto-creates from config)
2. Toast auto-subscribes to `OnAchievementEarned` events
3. Can be fully disabled via `OvationConfig.enableDefaultToast = false`

**Customization via ScriptableObject:**
- Background color
- Text color
- Font (TMP font asset)
- Position (top-left, top-right, bottom-left, bottom-right)
- Animation style (slide, fade, pop)
- Display duration
- Sound effect (optional AudioClip)

**Toast is entirely optional** — the SDK works perfectly without it. It's a convenience for developers who want quick results.

### Error Handling

The SDK should define a clear error type:

```csharp
public class OvationError
{
    public string Code { get; set; }        // Machine-readable (e.g., "achievement_not_found")
    public string Message { get; set; }     // Human-readable
    public int HttpStatusCode { get; set; } // HTTP status
    public bool IsNetworkError { get; set; } // True if the request never reached the server
}
```

**Retry behavior for network errors:**
- The SDK should NOT auto-retry most requests (let the game decide)
- Exception: `IssueAchievement` failures go to the offline queue (as specified above)
- All other network errors are surfaced to the caller via the error callback

### Logging

Use Unity's `Debug.Log` / `Debug.LogWarning` / `Debug.LogError` with an `[Ovation]` prefix:

```
[Ovation] Player created: e2f3a4b5-c6d7-8901-ef23-456789abcdef
[Ovation] Achievement issued: first-blood (was_new: true)
[Ovation] ERROR: API call failed (401): authentication_failed
```

All logging gated behind `OvationConfig.enableDebugLogging` (default: false in builds, true in editor).

---

## Important Implementation Notes

1. **Thread Safety** — Unity is single-threaded. All UnityWebRequest calls must run on the main thread (via coroutines or Unity's async context). Never use `Task.Run()` or background threads for API calls.

2. **Assembly Definitions** — Use `.asmdef` files so the SDK compiles as a separate assembly. This improves compilation times and prevents naming conflicts.

3. **No IL2CPP Issues** — Newtonsoft Json.NET works with IL2CPP (Unity's AOT compiler for builds) when using the Unity package version. The `link.xml` file may be needed to prevent code stripping.

4. **PlayerPrefs Keys** — Use a consistent prefix: `Ovation_PlayerId`, `Ovation_Environment`, etc.

5. **Namespace** — All SDK code under the `Ovation` namespace. Sub-namespaces: `Ovation.Api`, `Ovation.Models`, `Ovation.Cache`, `Ovation.UI`.

6. **Null Safety** — C# nullable reference types should be used where appropriate. Nullable fields in API responses (like `description`, `rarity_percentage`) must be represented as nullable in C# models.

7. **DateTime Handling** — All dates from the API are ISO 8601 with timezone. Use `DateTimeOffset` in C# models.
