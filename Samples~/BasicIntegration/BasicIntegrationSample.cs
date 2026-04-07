// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using Ovation;
using Ovation.Models;
using Ovation.UI;
using UnityEngine;

namespace Ovation.Samples
{
    /// <summary>
    /// Basic integration sample showing how to use the Ovation SDK.
    ///
    /// OPTION A (simplest — no scene setup):
    ///   Just attach this script. It calls OvationSDK.Init() and OvationSDK.Unlock().
    ///
    /// OPTION B (scene-based):
    ///   Add OvationSDK component to a GameObject, assign an OvationConfig asset.
    ///   Then use OvationSDK.Instance.IssueAchievement() etc.
    /// </summary>
    public class BasicIntegrationSample : MonoBehaviour
    {
        [Header("Setup")]
        [Tooltip("Your API key. In production, fetch this from your backend — never embed in builds.")]
        [SerializeField] private string apiKey = "ovn_test_YOUR_KEY_HERE";

        [Header("Test Settings")]
        [SerializeField] private string testAchievementSlug = "first-blood";

        private async void Start()
        {
            // --- THE SIMPLEST POSSIBLE INTEGRATION ---
            // Two lines. That's it.
            await OvationSDK.Init(apiKey, enableDebugLogging: true);
            AchievementToast.Create(); // Optional: shows popups when achievements unlock

            Debug.Log($"Ovation ready! Player ID: {OvationSDK.Instance.PlayerId}");
        }

        /// <summary>
        /// One line of code to unlock an achievement.
        /// Call this from a UI button, game event, trigger, etc.
        /// </summary>
        public void UnlockAchievement()
        {
            // This is the pitch: achievements in one line of code.
            OvationSDK.Unlock(testAchievementSlug);
        }

        /// <summary>
        /// Same thing with async/await if you want the result.
        /// </summary>
        public async void UnlockWithResult()
        {
            try
            {
                var result = await OvationSDK.UnlockAsync(testAchievementSlug);

                if (result.WasQueued)
                    Debug.Log($"Offline — '{result.Slug}' queued for sync");
                else if (result.WasNew)
                    Debug.Log($"NEW: {result.DisplayName} unlocked!");
                else
                    Debug.Log($"Already earned: {result.DisplayName}");
            }
            catch (OvationException ex)
            {
                Debug.LogError($"Failed: {ex.Error.Message}");
            }
        }

        /// <summary>
        /// List all achievements defined by the authority.
        /// </summary>
        public async void ListAchievements()
        {
            var achievements = await OvationSDK.Instance.GetAchievementsAsync();
            Debug.Log($"Found {achievements.Count} achievements:");
            foreach (var a in achievements)
                Debug.Log($"  {a.Slug}: {a.DisplayName} ({a.RarityPercentage}% earned)");
        }

        /// <summary>
        /// Show what the current player has earned.
        /// </summary>
        public async void ShowPlayerAchievements()
        {
            var earned = await OvationSDK.Instance.GetPlayerAchievementsAsync();
            Debug.Log($"Player has earned {earned.Count} achievements:");
            foreach (var a in earned)
            {
                Debug.Log($"  {a.DisplayName} (from {a.AuthorityName}) earned at {a.EarnedAt}");
                foreach (var asset in a.Assets)
                    Debug.Log($"    Asset: {asset.DisplayName} [{asset.SlotName}]");
            }
        }

        /// <summary>
        /// Link a platform ID (Steam, Xbox, etc.) to the current player.
        /// </summary>
        public async void LinkSteamId()
        {
            var result = await OvationSDK.Instance.SetExternalIdAsync("steam_76561198012345");
            Debug.Log($"Linked: {result.ExternalId}");
        }

        // --- ALTERNATIVE: Full API with callbacks (for devs who prefer that style) ---

        public void UnlockWithCallbacks()
        {
            OvationSDK.Instance.IssueAchievement(testAchievementSlug,
                result =>
                {
                    if (result.WasNew)
                        Debug.Log($"Unlocked: {result.DisplayName}!");
                },
                error => Debug.LogError($"Failed: {error.Message}")
            );
        }
    }
}
