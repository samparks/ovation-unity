# Ovation Unity SDK

The fastest way to add achievements to your Unity game. One line of code. Production backend included.

## Quick Start

### Install

In the Unity Editor: **Window > Package Manager > + > Add package from git URL**

```
https://github.com/Ovation-Games/ovation-unity.git
```

### Initialize

```csharp
using Ovation;
using Ovation.UI;

async void Start()
{
    // Initialize with your API key (fetch from your backend — never embed in builds)
    await OvationSDK.Init("your-api-key", enableDebugLogging: true);

    // Optional: show toast popups when achievements unlock
    AchievementToast.Create();
}
```

### Unlock achievements

```csharp
// That's it. One line.
OvationSDK.Unlock("first-blood");
```

## What you get

- **Achievements in one line of code** — `OvationSDK.Unlock("first-blood")`
- **Production backend** — no database, no server, no infrastructure to manage
- **Automatic player identity** — anonymous player IDs created and persisted automatically
- **Offline support** — achievements queue locally when offline, sync when connectivity returns
- **Built-in toast UI** — Steam-quality achievement popups, fully customizable or easy to disable
- **Asset caching** — achievement images downloaded and cached to disk with LRU eviction
- **Dual async API** — both callbacks and async/await for every operation
- **Editor dev tools** — test achievements, inspect player state, and reset data without leaving Unity

## Requirements

- Unity 2022.3 LTS or later
- Newtonsoft Json.NET (automatically installed via `com.unity.nuget.newtonsoft-json`)

## Setup Options

### Option A: Code only (simplest)

No scene setup needed. Call `OvationSDK.Init()` from any MonoBehaviour:

```csharp
await OvationSDK.Init("your-api-key");
OvationSDK.Unlock("first-blood");
```

### Option B: Scene-based (inspector-configurable)

1. Create a config asset: **Assets > Create > Ovation > Config**
2. Add an empty GameObject to your scene
3. Add the `OvationSDK` component and assign your config asset
4. The SDK initializes automatically on `Start()`

```csharp
// Then use the instance directly
OvationSDK.Instance.IssueAchievement("first-blood",
    result => Debug.Log($"Unlocked: {result.DisplayName}"),
    error => Debug.LogError(error.Message)
);
```

## API Reference

### Static API (`OvationSDK`)

The simplest way to use the SDK. These static methods work without any scene setup.

| Method | Description |
|--------|-------------|
| `OvationSDK.Init(apiKey, baseUrl?, enableDebugLogging?)` | Initialize the SDK. Call once at game start. |
| `OvationSDK.Unlock(slug)` | Issue an achievement to the current player. |
| `OvationSDK.UnlockAsync(slug)` | Issue an achievement and await the result. |
| `OvationSDK.Instance.GetAchievementsAsync()` | List all achievements defined by your authority. |
| `OvationSDK.Instance.GetPlayerAchievementsAsync()` | List achievements earned by the current player. |
| `OvationSDK.Instance.SetExternalIdAsync(externalId)` | Link a platform ID (Steam, Xbox, etc.) to the player. |
| `OvationSDK.Instance.PlayerId` | The current player's Ovation UUID. |
| `OvationSDK.IsReady` | Whether the SDK is initialized. |

### Instance API (`OvationSDK.Instance`)

Full API with both callback and async/await variants. See `OvationSDK.cs` for complete documentation.

**Achievement Operations:**
- `IssueAchievement(slug, onSuccess?, onError?, idempotencyKey?)`
- `GetAchievements(onSuccess, onError?)`
- `GetAchievement(slug, onSuccess, onError?)`
- `GetPlayerAchievements(onSuccess, onError?)`

**Slot & Equipment:**
- `GetStandardSlots(onSuccess, onError?)`
- `GetEquippedAssets(onSuccess, onError?)`
- `EquipAsset(slotId, assetId, onSuccess?, onError?)`
- `UnequipAsset(slotId, onSuccess?, onError?)`
- `GetEquippedAsset(slotName, onSuccess, onError?)`

**Assets:**
- `LoadAssetTexture(url, version, onLoaded)` — download and cache as `Texture2D`
- `LoadAssetSprite(url, version, onLoaded)` — download and cache as `Sprite`

**Events:**
- `OnAchievementEarned` — fires when a new achievement is unlocked (`WasNew == true`)
- `OnError` — fires on any API error
- `OnQueuedAchievementSynced` — fires when an offline-queued achievement syncs successfully

### Achievement Toast

Built-in popup notifications when achievements are unlocked.

```csharp
// Default setup — works out of the box
AchievementToast.Create();

// Custom configuration
var config = ScriptableObject.CreateInstance<AchievementToastConfig>();
config.position = ToastPosition.TopCenter;
config.displayDuration = 5f;
config.backgroundColor = new Color(0.1f, 0.1f, 0.2f, 0.95f);
AchievementToast.Create(config);
```

Or create a config asset in the editor: **Assets > Create > Ovation > Toast Config**

## Editor Tools

### Setup Wizard

**Ovation > Setup Wizard** — step-by-step guide to create your config asset.

### Dev Tools

**Ovation > Dev Tools** — runtime testing panel (available in Play Mode):
- View SDK status and player ID
- Issue achievements by slug with one click
- Browse all authority achievements
- View player achievement history
- Link external IDs
- Reset player identity
- Delete all test data

## Offline Support

When `IssueAchievement` fails due to a network error, the request is automatically queued to disk. The queue:
- Persists across app restarts (saved to `Application.persistentDataPath`)
- Retries with exponential backoff (up to 5 attempts)
- Uses idempotency keys to prevent duplicate issuance
- Holds up to 100 items (configurable)
- Flushes automatically every 60 seconds and after any successful API call

## API Key Security

Your API key should **never** be embedded in the game binary. Fetch it from your own backend at runtime:

```csharp
async void Start()
{
    string apiKey = await YourBackend.FetchOvationKey();
    await OvationSDK.Init(apiKey);
}
```

The `OvationConfig` inspector field is for **editor testing only**.

## Package Structure

```
Runtime/
  Ovation.cs              — (reserved)
  OvationSDK.cs           — Core singleton (MonoBehaviour)
  OvationConfig.cs        — ScriptableObject configuration
  Api/                    — HTTP client and service layer
  Models/                 — C# data models for API responses
  Cache/                  — Asset image download and disk cache
  Queue/                  — Offline achievement queue
  Identity/               — Automatic player ID management
  UI/                     — Achievement toast notification
  Utils/                  — Logging and utilities
Editor/
  OvationConfigEditor.cs  — Custom inspector for config
  OvationSetupWizard.cs   — First-time setup wizard
  OvationDevTools.cs      — Runtime testing panel
Tests/
  Editor/                 — Config validation tests
  Runtime/                — Model deserialization tests
  Integration~/           — API integration tests (standalone .NET, no Unity required)
```

## Integration Tests

The SDK includes integration tests that run against the live API **without Unity**. They use a standalone .NET test project that compiles the SDK's service layer with a non-Unity HTTP client.

```bash
cd "Tests/Integration~"
cp test-config.example.json test-config.json
# Edit test-config.json with your ovn_test_* API key
dotnet test
```

Requires [.NET 8+](https://dotnet.microsoft.com/download). See [`Tests/Integration~/README.md`](Tests/Integration~/README.md) for details.

## License

MIT License. See [LICENSE](LICENSE) for details.

## Links

- [Ovation Website](https://ovation.games)
- [Authority Portal](https://app.ovation.games) — register and manage your achievements
- [API Documentation](https://docs.ovation.games)
