# Integration Tests

API integration tests that run against the live Ovation API (`api.ovation.games`) **without requiring Unity**.

## How it works

The SDK's service layer (`AchievementService`, `AssetService`, etc.) depends on an `IOvationHttpClient` interface, not the concrete Unity-based `OvationApiClient`. This test project provides `StandardHttpClient` — an implementation backed by `System.Net.Http.HttpClient` — so the same service code can run in a plain .NET environment.

The `.csproj` **links** (not copies) the SDK's source files so tests compile against the real service and model code. A few files that depend on Unity types (`OvationApiClient`, `OvationLogger`) are replaced with lightweight stubs in `Helpers/`.

## Setup

1. Install [.NET 8+](https://dotnet.microsoft.com/download)
2. Copy the example config and add your test API key:
   ```
   cp test-config.example.json test-config.json
   ```
3. Edit `test-config.json` with your `ovn_test_*` key

## Run

```bash
cd Tests/Integration
dotnet test
```

## What's tested

- Achievement listing, fetching by slug, and slot-asset bindings
- Asset metadata retrieval and image download verification
- Player creation, achievement issuance, and earned asset summaries
- Error handling for non-existent resources (404s)
- Full referential integrity: achievement SlotAssets point to valid assets in the correct slots
