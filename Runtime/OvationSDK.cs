// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ovation.Api;
using Ovation.Cache;
using Ovation.Identity;
using Ovation.Models;
using Ovation.Queue;
using Ovation.Utils;
using UnityEngine;

namespace Ovation
{
    /// <summary>
    /// Core Ovation SDK singleton. Persists across scenes via DontDestroyOnLoad.
    ///
    /// There are two ways to set up the SDK:
    ///
    /// 1. Code-only (simplest): Call <see cref="Ovation.Init"/> which creates this automatically.
    /// 2. Scene-based: Add this component to a GameObject and assign an <see cref="OvationConfig"/> asset.
    ///
    /// All public methods have two variants:
    /// - Callback: <c>GetAchievements(onSuccess, onError)</c>
    /// - Async/await: <c>await GetAchievementsAsync()</c>
    ///
    /// Async methods throw <see cref="OvationException"/> on API errors.
    /// Callback methods invoke the onError callback instead.
    /// Both variants fire the <see cref="OnError"/> event.
    /// </summary>
    public class OvationSDK : MonoBehaviour
    {
        private static OvationSDK _instance;
        public static OvationSDK Instance => _instance;

        [SerializeField] private OvationConfig config;

        // Internal services
        private OvationApiClient _apiClient;
        private PlayerService _playerService;
        private AchievementService _achievementService;
        private SlotService _slotService;
        private AssetService _assetService;
        private PlayerIdentityManager _identityManager;
        private OfflineQueue _offlineQueue;
        private AssetCache _assetCache;

        private bool _initialized;
        private float _lastQueueFlush;

        /// <summary>
        /// Fires when a new achievement is unlocked (WasNew == true). Does not fire for
        /// already-earned achievements or offline-queued requests.
        /// </summary>
        public event Action<IssueAchievementResult> OnAchievementEarned;

        /// <summary>
        /// Fires on any API error, in addition to per-call error callbacks.
        /// Useful for global error logging or monitoring.
        /// </summary>
        public event Action<OvationError> OnError;

        /// <summary>
        /// Fires when a previously offline-queued achievement is successfully synced to the server.
        /// Parameters: (achievement slug, issue result).
        /// </summary>
        public event Action<string, IssueAchievementResult> OnQueuedAchievementSynced;

        /// <summary>
        /// The current player's Ovation ID. Null if not yet initialized.
        /// </summary>
        public string PlayerId => _identityManager?.PlayerId;

        /// <summary>
        /// Whether the SDK has been fully initialized.
        /// </summary>
        public bool IsInitialized => _initialized;

        /// <summary>
        /// Number of achievement issuance requests waiting in the offline queue.
        /// </summary>
        public int OfflineQueueCount => _offlineQueue?.Count ?? 0;

        /// <summary>
        /// Create the OvationSDK singleton programmatically — no scene setup needed.
        /// Use Ovation.Init() instead of calling this directly.
        /// </summary>
        internal static OvationSDK CreateProgrammatic(string apiKey, string baseUrl = null, bool enableDebugLogging = false)
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject("[Ovation SDK]");
            var sdk = go.AddComponent<OvationSDK>();

            // Create a runtime config
            var runtimeConfig = ScriptableObject.CreateInstance<OvationConfig>();
            runtimeConfig.SetApiKey(apiKey);
            if (!string.IsNullOrEmpty(baseUrl))
                runtimeConfig.SetBaseUrlOverride(baseUrl);
            runtimeConfig.SetDebugLogging(enableDebugLogging);

            sdk.config = runtimeConfig;
            sdk._programmaticInit = true;
            return sdk;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            if (config == null)
            {
                OvationLogger.Error("OvationConfig not assigned. Drag an OvationConfig asset onto the OvationSDK component, or call Ovation.Init().");
                return;
            }

            // Don't auto-init if created programmatically — Init() calls InitializeAsync() explicitly
            if (!_initialized && !_programmaticInit)
                await InitializeAsync();
        }

        private bool _programmaticInit;

        private void Update()
        {
            if (!_initialized || _offlineQueue == null)
                return;

            // Periodic queue flush
            if (Time.realtimeSinceStartup - _lastQueueFlush >= config.QueueFlushIntervalSeconds)
            {
                _lastQueueFlush = Time.realtimeSinceStartup;
                _ = _offlineQueue.FlushAsync();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        /// <summary>
        /// Initialize the SDK manually. Called automatically on Start if config is assigned.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            if (config == null)
            {
                OvationLogger.Error("Cannot initialize: OvationConfig is null.");
                return;
            }

            OvationLogger.SetEnabled(config.EnableDebugLogging);
            OvationLogger.Log($"Initializing Ovation SDK v{Application.version} | Base URL: {config.BaseUrl}");

            // Setup API client
            _apiClient = new OvationApiClient(config.BaseUrl);
            if (!string.IsNullOrEmpty(config.ApiKey))
                _apiClient.SetApiKey(config.ApiKey);

            // Setup services
            _playerService = new PlayerService(_apiClient);
            _achievementService = new AchievementService(_apiClient);
            _slotService = new SlotService(_apiClient);
            _assetService = new AssetService(_apiClient);

            // Setup identity
            _identityManager = new PlayerIdentityManager(_playerService);
            if (config.AutoManagePlayerId)
                await _identityManager.EnsurePlayerIdAsync();

            // Setup offline queue
            _offlineQueue = new OfflineQueue(_playerService, config.MaxQueueSize);
            _offlineQueue.OnQueuedAchievementSynced += (slug, result) =>
            {
                OnQueuedAchievementSynced?.Invoke(slug, result);
            };

            // Setup asset cache
            _assetCache = new AssetCache(_apiClient, config.MaxCacheSizeMB);

            // Flush any pending queued items
            _ = _offlineQueue.FlushAsync();

            _initialized = true;
            OvationLogger.Log("Ovation SDK initialized successfully");
        }

        /// <summary>
        /// Set the API key at runtime. Call this after fetching the key from your backend.
        /// </summary>
        public void SetApiKey(string apiKey)
        {
            config.SetApiKey(apiKey);
            _apiClient?.SetApiKey(apiKey);
            OvationLogger.Log("API key updated");
        }

        /// <summary>
        /// Set a known player ID. Use when managing player IDs yourself (autoManagePlayerId = false).
        /// </summary>
        public void SetPlayerId(string playerId)
        {
            EnsureInitialized();
            _identityManager.SetPlayerId(playerId);
        }

        /// <summary>
        /// Link a platform-specific external ID to the current player (e.g., Steam ID).
        /// </summary>
        public async void SetExternalId(string externalId, Action<ExternalIdResponse> onSuccess = null, Action<OvationError> onError = null)
        {
            EnsureInitialized();
            EnsurePlayerId();

            var result = await _playerService.SetExternalIdAsync(PlayerId, externalId);
            HandleResult(result, onSuccess, onError);
        }

        /// <summary>
        /// Set external ID (async variant).
        /// </summary>
        public async Task<ExternalIdResponse> SetExternalIdAsync(string externalId)
        {
            EnsureInitialized();
            EnsurePlayerId();

            var result = await _playerService.SetExternalIdAsync(PlayerId, externalId);
            return HandleResultAsync(result);
        }

        // ----- Achievement Operations -----

        /// <summary>
        /// Issue an achievement to the current player.
        /// </summary>
        public async void IssueAchievement(string slug, Action<IssueAchievementResult> onSuccess = null, Action<OvationError> onError = null, string idempotencyKey = null)
        {
            EnsureInitialized();
            EnsurePlayerId();

            var result = await _playerService.IssueAchievementAsync(PlayerId, slug, idempotencyKey);

            if (result.Success)
            {
                onSuccess?.Invoke(result.Data);
                if (result.Data.WasNew)
                {
                    try { OnAchievementEarned?.Invoke(result.Data); }
                    catch (Exception ex) { OvationLogger.Warning($"Error in OnAchievementEarned handler: {ex.Message}"); }
                }
            }
            else if (result.Error.IsNetworkError)
            {
                // Queue for later
                _offlineQueue.Enqueue(slug, PlayerId, idempotencyKey ?? IdempotencyKeyGenerator.Generate());
                var queuedResult = new IssueAchievementResult
                {
                    Slug = slug,
                    WasQueued = true
                };
                onSuccess?.Invoke(queuedResult);
            }
            else
            {
                onError?.Invoke(result.Error);
                try { OnError?.Invoke(result.Error); }
                catch (Exception ex) { OvationLogger.Warning($"Error in OnError handler: {ex.Message}"); }
            }
        }

        /// <summary>
        /// Issue an achievement (async variant). Returns queued result if offline.
        /// </summary>
        public async Task<IssueAchievementResult> IssueAchievementAsync(string slug, string idempotencyKey = null)
        {
            EnsureInitialized();
            EnsurePlayerId();

            var result = await _playerService.IssueAchievementAsync(PlayerId, slug, idempotencyKey);

            if (result.Success)
            {
                if (result.Data.WasNew)
                {
                    try { OnAchievementEarned?.Invoke(result.Data); }
                    catch (Exception ex) { OvationLogger.Warning($"Error in OnAchievementEarned handler: {ex.Message}"); }
                }
                return result.Data;
            }

            if (result.Error.IsNetworkError)
            {
                _offlineQueue.Enqueue(slug, PlayerId, idempotencyKey ?? IdempotencyKeyGenerator.Generate());
                return new IssueAchievementResult { Slug = slug, WasQueued = true };
            }

            try { OnError?.Invoke(result.Error); }
            catch (Exception ex) { OvationLogger.Warning($"Error in OnError handler: {ex.Message}"); }
            throw new OvationException(result.Error);
        }

        /// <summary>
        /// Get all achievements defined by the authority.
        /// </summary>
        public async void GetAchievements(Action<List<Achievement>> onSuccess, Action<OvationError> onError = null)
        {
            EnsureInitialized();
            var result = await _achievementService.GetAchievementsAsync();
            HandleResult(result, onSuccess, onError);
        }

        /// <summary>
        /// Get all achievements (async variant).
        /// </summary>
        public async Task<List<Achievement>> GetAchievementsAsync()
        {
            EnsureInitialized();
            var result = await _achievementService.GetAchievementsAsync();
            return HandleResultAsync(result);
        }

        /// <summary>
        /// Get a single achievement by slug.
        /// </summary>
        public async void GetAchievement(string slug, Action<Achievement> onSuccess, Action<OvationError> onError = null)
        {
            EnsureInitialized();
            var result = await _achievementService.GetAchievementAsync(slug);
            HandleResult(result, onSuccess, onError);
        }

        /// <summary>
        /// Get a single achievement by slug (async variant).
        /// </summary>
        public async Task<Achievement> GetAchievementAsync(string slug)
        {
            EnsureInitialized();
            var result = await _achievementService.GetAchievementAsync(slug);
            return HandleResultAsync(result);
        }

        /// <summary>
        /// Get the current player's earned achievements.
        /// </summary>
        public async void GetPlayerAchievements(Action<List<PlayerAchievement>> onSuccess, Action<OvationError> onError = null)
        {
            EnsureInitialized();
            EnsurePlayerId();
            var result = await _playerService.GetPlayerAchievementsAsync(PlayerId);
            HandleResult(result, onSuccess, onError);
        }

        /// <summary>
        /// Get the current player's earned achievements (async variant).
        /// </summary>
        public async Task<List<PlayerAchievement>> GetPlayerAchievementsAsync()
        {
            EnsureInitialized();
            EnsurePlayerId();
            var result = await _playerService.GetPlayerAchievementsAsync(PlayerId);
            return HandleResultAsync(result);
        }

        // ----- Slot & Equipment Operations -----

        /// <summary>
        /// Get all standard slots with their specifications.
        /// </summary>
        public async void GetStandardSlots(Action<List<Slot>> onSuccess, Action<OvationError> onError = null)
        {
            EnsureInitialized();
            var result = await _slotService.GetStandardSlotsAsync();
            HandleResult(result, onSuccess, onError);
        }

        /// <summary>
        /// Get all standard slots (async variant).
        /// </summary>
        public async Task<List<Slot>> GetStandardSlotsAsync()
        {
            EnsureInitialized();
            var result = await _slotService.GetStandardSlotsAsync();
            return HandleResultAsync(result);
        }

        /// <summary>
        /// Get all equipped assets for the current player.
        /// </summary>
        public async void GetEquippedAssets(Action<List<EquippedSlotResponse>> onSuccess, Action<OvationError> onError = null)
        {
            EnsureInitialized();
            EnsurePlayerId();
            var result = await _slotService.GetEquippedAssetsAsync(PlayerId);
            HandleResult(result, onSuccess, onError);
        }

        /// <summary>
        /// Get all equipped assets (async variant).
        /// </summary>
        public async Task<List<EquippedSlotResponse>> GetEquippedAssetsAsync()
        {
            EnsureInitialized();
            EnsurePlayerId();
            var result = await _slotService.GetEquippedAssetsAsync(PlayerId);
            return HandleResultAsync(result);
        }

        /// <summary>
        /// Equip an asset in a slot for the current player.
        /// </summary>
        public async void EquipAsset(string slotId, string assetId, Action<EquippedSlotResponse> onSuccess = null, Action<OvationError> onError = null)
        {
            EnsureInitialized();
            EnsurePlayerId();
            var result = await _slotService.EquipAssetAsync(slotId, PlayerId, assetId);
            HandleResult(result, onSuccess, onError);
        }

        /// <summary>
        /// Equip an asset (async variant).
        /// </summary>
        public async Task<EquippedSlotResponse> EquipAssetAsync(string slotId, string assetId)
        {
            EnsureInitialized();
            EnsurePlayerId();
            var result = await _slotService.EquipAssetAsync(slotId, PlayerId, assetId);
            return HandleResultAsync(result);
        }

        /// <summary>
        /// Unequip an asset from a slot for the current player.
        /// </summary>
        public async void UnequipAsset(string slotId, Action onSuccess = null, Action<OvationError> onError = null)
        {
            EnsureInitialized();
            EnsurePlayerId();
            var result = await _slotService.UnequipAssetAsync(slotId, PlayerId);
            if (result.Success)
                onSuccess?.Invoke();
            else
            {
                onError?.Invoke(result.Error);
                try { OnError?.Invoke(result.Error); }
                catch (Exception ex) { OvationLogger.Warning($"Error in OnError handler: {ex.Message}"); }
            }
        }

        /// <summary>
        /// Unequip an asset (async variant).
        /// </summary>
        public async Task UnequipAssetAsync(string slotId)
        {
            EnsureInitialized();
            EnsurePlayerId();
            var result = await _slotService.UnequipAssetAsync(slotId, PlayerId);
            if (!result.Success)
            {
                try { OnError?.Invoke(result.Error); }
                catch (Exception ex) { OvationLogger.Warning($"Error in OnError handler: {ex.Message}"); }
                throw new OvationException(result.Error);
            }
        }

        /// <summary>
        /// Get what the current player has equipped in a specific slot.
        /// </summary>
        public async void GetEquippedAsset(string slotName, Action<EquippedSlotResponse> onSuccess, Action<OvationError> onError = null)
        {
            EnsureInitialized();
            EnsurePlayerId();
            var result = await _slotService.GetEquippedAssetAsync(slotName, PlayerId);
            HandleResult(result, onSuccess, onError);
        }

        /// <summary>
        /// Get equipped asset for a slot (async variant).
        /// </summary>
        public async Task<EquippedSlotResponse> GetEquippedAssetAsync(string slotName)
        {
            EnsureInitialized();
            EnsurePlayerId();
            var result = await _slotService.GetEquippedAssetAsync(slotName, PlayerId);
            return HandleResultAsync(result);
        }

        // ----- Asset Operations -----

        /// <summary>
        /// Get asset details by ID.
        /// </summary>
        public async void GetAsset(string assetId, Action<Asset> onSuccess, Action<OvationError> onError = null)
        {
            EnsureInitialized();
            var result = await _assetService.GetAssetAsync(assetId);
            HandleResult(result, onSuccess, onError);
        }

        /// <summary>
        /// Get asset details (async variant).
        /// </summary>
        public async Task<Asset> GetAssetAsync(string assetId)
        {
            EnsureInitialized();
            var result = await _assetService.GetAssetAsync(assetId);
            return HandleResultAsync(result);
        }

        /// <summary>
        /// Download and cache an asset image as a Texture2D.
        /// </summary>
        public async void LoadAssetTexture(string url, int version, Action<Texture2D> onLoaded)
        {
            EnsureInitialized();
            var texture = await _assetCache.LoadTextureAsync(url, version);
            onLoaded?.Invoke(texture);
        }

        /// <summary>
        /// Load asset texture (async variant).
        /// </summary>
        public async Task<Texture2D> LoadAssetTextureAsync(string url, int version)
        {
            EnsureInitialized();
            return await _assetCache.LoadTextureAsync(url, version);
        }

        /// <summary>
        /// Download and cache an asset image as a Sprite.
        /// </summary>
        public async void LoadAssetSprite(string url, int version, Action<Sprite> onLoaded)
        {
            EnsureInitialized();
            var sprite = await _assetCache.LoadSpriteAsync(url, version);
            onLoaded?.Invoke(sprite);
        }

        /// <summary>
        /// Load asset sprite (async variant).
        /// </summary>
        public async Task<Sprite> LoadAssetSpriteAsync(string url, int version)
        {
            EnsureInitialized();
            return await _assetCache.LoadSpriteAsync(url, version);
        }

        // ----- Authority -----

        /// <summary>
        /// Get the authority profile for the current API key.
        /// </summary>
        public async void GetAuthority(Action<Authority> onSuccess, Action<OvationError> onError = null)
        {
            EnsureInitialized();
            var result = await _apiClient.GetAsync<Authority>("/authority");
            HandleResult(result, onSuccess, onError);
        }

        /// <summary>
        /// Get authority profile (async variant).
        /// </summary>
        public async Task<Authority> GetAuthorityAsync()
        {
            EnsureInitialized();
            var result = await _apiClient.GetAsync<Authority>("/authority");
            return HandleResultAsync(result);
        }

        /// <summary>
        /// Wipe all test data (only works with test API keys).
        /// </summary>
        public async void DeleteTestData(Action onSuccess = null, Action<OvationError> onError = null)
        {
            EnsureInitialized();
            var result = await _apiClient.DeleteAsync("/authority/test-data");
            if (result.Success)
                onSuccess?.Invoke();
            else
            {
                onError?.Invoke(result.Error);
                try { OnError?.Invoke(result.Error); }
                catch (Exception ex) { OvationLogger.Warning($"Error in OnError handler: {ex.Message}"); }
            }
        }

        // ----- Private Helpers -----

        private void EnsureInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException("[Ovation] SDK is not initialized. Ensure OvationSDK is in your scene with a valid OvationConfig.");
        }

        private void EnsurePlayerId()
        {
            if (string.IsNullOrEmpty(PlayerId))
                throw new InvalidOperationException("[Ovation] No player ID available. Ensure autoManagePlayerId is enabled or call SetPlayerId() first.");
        }

        private void HandleResult<T>(ApiResult<T> result, Action<T> onSuccess, Action<OvationError> onError)
        {
            if (result.Success)
            {
                onSuccess?.Invoke(result.Data);
            }
            else
            {
                onError?.Invoke(result.Error);
                try { OnError?.Invoke(result.Error); }
                catch (Exception ex) { OvationLogger.Warning($"Error in OnError handler: {ex.Message}"); }
            }
        }

        private T HandleResultAsync<T>(ApiResult<T> result)
        {
            if (result.Success)
                return result.Data;

            try { OnError?.Invoke(result.Error); }
            catch (Exception ex) { OvationLogger.Warning($"Error in OnError handler: {ex.Message}"); }
            throw new OvationException(result.Error);
        }
    }

    public class OvationException : Exception
    {
        public OvationError Error { get; }

        public OvationException(OvationError error) : base(error.ToString())
        {
            Error = error;
        }
    }
}
