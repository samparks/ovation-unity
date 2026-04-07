// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using UnityEngine;

namespace Ovation
{
    /// <summary>
    /// ScriptableObject holding all Ovation SDK configuration. Create via Assets > Create > Ovation > Config
    /// and assign to the OvationSDK component, or use <see cref="Ovation.Init"/> for code-only setup.
    /// </summary>
    [CreateAssetMenu(fileName = "OvationConfig", menuName = "Ovation/Config", order = 1)]
    public class OvationConfig : ScriptableObject
    {
        [Header("Authentication")]
        [Tooltip("Your Ovation API key. Fetch this from your backend at runtime — never embed in builds.")]
        [SerializeField] private string apiKey;

        [Header("Environment")]
        [Tooltip("Toggle between test and live environments. Test keys create isolated test data.")]
        [SerializeField] private OvationEnvironment environment = OvationEnvironment.Test;

        [Tooltip("Override the base URL. Leave empty to use the default for the selected environment.")]
        [SerializeField] private string baseUrlOverride;

        [Header("Debug")]
        [Tooltip("Enable verbose logging with [Ovation] prefix. Defaults to true in the editor.")]
        [SerializeField] private bool enableDebugLogging = true;

        [Header("Player Identity")]
        [Tooltip("If true, the SDK auto-creates and persists an anonymous player ID. Disable to manage player IDs yourself.")]
        [SerializeField] private bool autoManagePlayerId = true;

        [Header("Offline Queue")]
        [Tooltip("Maximum number of achievement issuance requests to queue when offline.")]
        [SerializeField] private int maxQueueSize = 100;

        [Tooltip("Seconds between offline queue flush attempts.")]
        [SerializeField] private float queueFlushIntervalSeconds = 60f;

        [Header("Asset Cache")]
        [Tooltip("Maximum cache size in megabytes for downloaded asset images.")]
        [SerializeField] private int maxCacheSizeMB = 50;

        /// <summary>
        /// Default API base URL.
        /// </summary>
        public const string DefaultBaseUrl = "https://api.ovation.games";

        public string ApiKey => apiKey;
        public OvationEnvironment Environment => environment;
        public bool EnableDebugLogging => enableDebugLogging;
        public bool AutoManagePlayerId => autoManagePlayerId;
        public int MaxQueueSize => maxQueueSize;
        public float QueueFlushIntervalSeconds => queueFlushIntervalSeconds;
        public int MaxCacheSizeMB => maxCacheSizeMB;

        public string BaseUrl
        {
            get
            {
                if (!string.IsNullOrEmpty(baseUrlOverride))
                    return baseUrlOverride.TrimEnd('/');
                return DefaultBaseUrl;
            }
        }

        /// <summary>
        /// Set the API key at runtime. Use this when fetching the key from your backend.
        /// </summary>
        public void SetApiKey(string key)
        {
            apiKey = key;
        }

        /// <summary>
        /// Set the base URL override at runtime.
        /// </summary>
        public void SetBaseUrlOverride(string url)
        {
            baseUrlOverride = url;
        }

        /// <summary>
        /// Set debug logging at runtime.
        /// </summary>
        public void SetDebugLogging(bool enabled)
        {
            enableDebugLogging = enabled;
        }
    }

    /// <summary>
    /// Ovation API environment. Test keys (ovn_test_*) create isolated test data.
    /// Live keys (ovn_live_*) create real data.
    /// </summary>
    public enum OvationEnvironment
    {
        /// <summary>Test environment — data is isolated and can be wiped via DeleteTestData().</summary>
        Test,
        /// <summary>Live environment — creates real, persistent data.</summary>
        Live
    }
}
