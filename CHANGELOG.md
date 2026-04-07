# Changelog

All notable changes to this project will be documented in this file.

## [0.2.1] - 2026-04-07

### Fixed
- **Default API base URL now points to production** (`https://api.ovation.games`) instead of the dev environment. External users were silently hitting the dev API, causing authentication failures with production API keys. This was the primary reason the SDK was not working for users who cloned the repo.
- **Documentation URL** in `package.json` updated from dead `docs.ovation.games` to `ovation.mintlify.app`. The UPM "View documentation" link was leading to a page that doesn't resolve.
- **API Documentation link** in README updated to `ovation.mintlify.app`.
- All internal documentation (`Documentation~/`, doc comments) updated to reference the production URL instead of dev.
- Unit test for default base URL updated to assert the production URL.

### Added
- `Rarity` property on `IssueAchievementResult` model — the API returns a rarity tier string (`"Common"`, `"Uncommon"`, `"Rare"`, `"Epic"`, `"Legendary"`) when issuing achievements, but the SDK was silently dropping it.
- **30-second request timeout** on all HTTP calls (`GET`, `POST`, `PUT`, `DELETE`, asset downloads). Previously, requests had no timeout — if the server was unreachable, the game would hang indefinitely with no error feedback.

## [0.2.0] - 2026-03-26

### Added
- `IOvationHttpClient` interface extracted from `OvationApiClient` for testability
- `GetAllPagesAsync` pagination logic moved to an extension method on the interface
- Standalone .NET integration test project (`Tests/Integration/`) that runs against the live API without Unity
- `StandardHttpClient` — a `System.Net.Http.HttpClient`-based implementation of `IOvationHttpClient` for testing outside Unity
- 12 integration tests covering achievements, assets, slot-asset bindings, player achievement issuance, and error handling

### Changed
- All services (`AchievementService`, `AssetService`, `PlayerService`, `SlotService`) and `AssetCache` now depend on `IOvationHttpClient` instead of the concrete `OvationApiClient`
- `OvationSDK._apiClient` field typed as `IOvationHttpClient` (concrete type only used at the composition root)

### Fixed
- `package.json` repository URLs now point to the correct GitHub organization

## [0.1.0] - 2026-03-15

### Added
- Initial SDK release
- OvationSDK singleton with DontDestroyOnLoad persistence
- OvationConfig ScriptableObject for drag-and-drop configuration
- Automatic anonymous player identity management via PlayerPrefs
- Achievement operations: list, get by slug, issue to player
- Player achievement history with auto-pagination
- Slot and equipment operations: list standard slots, equip/unequip assets
- Asset details fetching (read-only)
- Asset image downloading with disk-based LRU cache
- Offline queue for achievement issuance with retry and idempotency
- Dual API pattern: callback and async/await for all operations
- C# events: OnAchievementEarned, OnError, OnQueuedAchievementSynced
- Editor tools: custom config inspector, setup wizard
- link.xml for IL2CPP compatibility
- Unit tests for model deserialization
- Basic integration sample
