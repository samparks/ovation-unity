// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System;
using Newtonsoft.Json;

namespace Ovation.Models
{
    /// <summary>
    /// Result of issuing an achievement to a player. Check <see cref="WasNew"/> to determine
    /// if this was a first-time unlock, and <see cref="WasQueued"/> to determine if the request
    /// was queued offline for later sync.
    /// </summary>
    [Serializable]
    public class IssueAchievementResult
    {
        /// <summary>The achievement's slug identifier.</summary>
        [JsonProperty("slug")]
        public string Slug { get; set; }

        /// <summary>Human-readable achievement name. Null if the request was queued offline.</summary>
        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        /// <summary>When the achievement was earned. Default if queued offline.</summary>
        [JsonProperty("earned_at")]
        public DateTimeOffset EarnedAt { get; set; }

        /// <summary>True if this is the first time the player earned this achievement. False if already earned (non-repeatable) or duplicate idempotency key.</summary>
        [JsonProperty("was_new")]
        public bool WasNew { get; set; }

        /// <summary>Rarity tier based on percentage of players who have earned this achievement (e.g., "Common", "Uncommon", "Rare", "Epic", "Legendary"). Null if queued offline.</summary>
        [JsonProperty("rarity")]
        public string Rarity { get; set; }

        /// <summary>
        /// True if the request failed due to a network error and was queued for offline sync.
        /// When true, other fields (DisplayName, EarnedAt, WasNew) may be unset.
        /// The queued request will be retried automatically when connectivity is restored.
        /// </summary>
        [JsonIgnore]
        public bool WasQueued { get; set; }
    }
}
