// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Ovation.Models
{
    /// <summary>
    /// An achievement earned by a player, including the issuing authority's name
    /// and any visual assets bound to the achievement.
    /// </summary>
    [Serializable]
    public class PlayerAchievement
    {
        /// <summary>The achievement's slug identifier.</summary>
        [JsonProperty("slug")]
        public string Slug { get; set; }

        /// <summary>Human-readable achievement name.</summary>
        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        /// <summary>Optional description of how to earn this achievement.</summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>UUID of the authority (game studio) that issued this achievement.</summary>
        [JsonProperty("authority_id")]
        public string AuthorityId { get; set; }

        /// <summary>Display name of the authority that issued this achievement.</summary>
        [JsonProperty("authority_name")]
        public string AuthorityName { get; set; }

        /// <summary>When the player earned this achievement.</summary>
        [JsonProperty("earned_at")]
        public DateTimeOffset EarnedAt { get; set; }

        /// <summary>Visual assets (badges, icons, etc.) bound to this achievement.</summary>
        [JsonProperty("assets")]
        public List<AssetSummary> Assets { get; set; } = new List<AssetSummary>();
    }
}
