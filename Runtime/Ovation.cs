// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ovation.Models;
using Ovation.Utils;
using UnityEngine;

namespace Ovation
{
    /// <summary>
    /// Static convenience API for the Ovation SDK.
    /// This is the simplest way to use Ovation — one line of code.
    ///
    /// Setup (call once at game start):
    ///   Ovation.Init("your-api-key");
    ///
    /// Then anywhere in your game:
    ///   Ovation.Unlock("first-blood");
    /// </summary>
    public static class Ovation
    {
        /// <summary>
        /// Initialize the Ovation SDK with just an API key.
        /// Creates the singleton automatically — no scene setup required.
        /// </summary>
        /// <param name="apiKey">Your Ovation API key (fetched from your backend)</param>
        /// <param name="baseUrl">Optional base URL override</param>
        /// <param name="enableDebugLogging">Enable [Ovation] debug logs</param>
        public static async Task Init(string apiKey, string baseUrl = null, bool enableDebugLogging = false)
        {
            if (OvationSDK.Instance != null && OvationSDK.Instance.IsInitialized)
            {
                OvationLogger.Log("SDK already initialized, updating API key");
                OvationSDK.Instance.SetApiKey(apiKey);
                return;
            }

            // Create the singleton programmatically
            var sdk = OvationSDK.CreateProgrammatic(apiKey, baseUrl, enableDebugLogging);
            await sdk.InitializeAsync();
        }

        /// <summary>
        /// Issue an achievement to the current player. One line of code.
        /// Queues automatically if offline. Fires OnAchievementEarned if new.
        /// </summary>
        public static void Unlock(string slug)
        {
            EnsureReady();
            OvationSDK.Instance.IssueAchievement(slug);
        }

        /// <summary>
        /// Issue an achievement (async). Returns the result with WasNew and WasQueued flags.
        /// </summary>
        public static Task<IssueAchievementResult> UnlockAsync(string slug)
        {
            EnsureReady();
            return OvationSDK.Instance.IssueAchievementAsync(slug);
        }

        /// <summary>
        /// Get all achievements defined by the authority.
        /// </summary>
        public static Task<List<Achievement>> GetAchievementsAsync()
        {
            EnsureReady();
            return OvationSDK.Instance.GetAchievementsAsync();
        }

        /// <summary>
        /// Get the current player's earned achievements.
        /// </summary>
        public static Task<List<PlayerAchievement>> GetPlayerAchievementsAsync()
        {
            EnsureReady();
            return OvationSDK.Instance.GetPlayerAchievementsAsync();
        }

        /// <summary>
        /// Link a platform-specific external ID (e.g., Steam ID) to the current player.
        /// </summary>
        public static Task<ExternalIdResponse> SetExternalIdAsync(string externalId)
        {
            EnsureReady();
            return OvationSDK.Instance.SetExternalIdAsync(externalId);
        }

        /// <summary>
        /// The current player's Ovation UUID. Auto-created on first init and persisted in PlayerPrefs.
        /// Throws if the SDK is not initialized — check <see cref="IsReady"/> first if unsure.
        /// </summary>
        public static string PlayerId
        {
            get
            {
                EnsureReady();
                return OvationSDK.Instance.PlayerId;
            }
        }

        /// <summary>
        /// True if the SDK is initialized and ready to accept calls. Safe to check without try/catch.
        /// </summary>
        public static bool IsReady => OvationSDK.Instance != null && OvationSDK.Instance.IsInitialized;

        /// <summary>
        /// Subscribe to achievement earned events.
        /// </summary>
        public static event Action<IssueAchievementResult> OnAchievementEarned
        {
            add
            {
                EnsureReady();
                OvationSDK.Instance.OnAchievementEarned += value;
            }
            remove
            {
                if (OvationSDK.Instance != null)
                    OvationSDK.Instance.OnAchievementEarned -= value;
            }
        }

        /// <summary>
        /// Subscribe to error events.
        /// </summary>
        public static event Action<OvationError> OnError
        {
            add
            {
                EnsureReady();
                OvationSDK.Instance.OnError += value;
            }
            remove
            {
                if (OvationSDK.Instance != null)
                    OvationSDK.Instance.OnError -= value;
            }
        }

        private static void EnsureReady()
        {
            if (OvationSDK.Instance == null || !OvationSDK.Instance.IsInitialized)
                throw new InvalidOperationException(
                    "[Ovation] SDK not initialized. Call Ovation.Init(\"your-api-key\") first, " +
                    "or add OvationSDK to your scene with a config asset.");
        }
    }
}
