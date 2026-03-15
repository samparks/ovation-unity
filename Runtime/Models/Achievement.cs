// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Ovation.Models
{
    /// <summary>
    /// An achievement defined by an authority. Achievements are identified by their slug
    /// (immutable, used in game code) and have a display name (human-readable, can be updated).
    /// </summary>
    [Serializable]
    public class Achievement
    {
        /// <summary>Unique identifier (UUID) for this achievement.</summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>Immutable slug identifier used in game code (e.g., "first-blood").</summary>
        [JsonProperty("slug")]
        public string Slug { get; set; }

        /// <summary>Human-readable name shown to players (e.g., "First Blood").</summary>
        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        /// <summary>Optional longer description of how to earn this achievement.</summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>If true, the same player can earn this achievement multiple times.</summary>
        [JsonProperty("repeatable")]
        public bool Repeatable { get; set; }

        /// <summary>If true, this achievement has been soft-deleted and can no longer be issued.</summary>
        [JsonProperty("archived")]
        public bool Archived { get; set; }

        /// <summary>If true, this achievement is hidden from players until they earn it.</summary>
        [JsonProperty("is_hidden")]
        public bool IsHidden { get; set; }

        /// <summary>Percentage of players who have earned this achievement (0-100), or null if not yet calculated.</summary>
        [JsonProperty("rarity_percentage")]
        public float? RarityPercentage { get; set; }

        /// <summary>Map of slot ID to asset ID for visual assets bound to this achievement.</summary>
        [JsonProperty("slot_assets")]
        public Dictionary<string, string> SlotAssets { get; set; }

        /// <summary>When this achievement was created.</summary>
        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>When this achievement was last updated.</summary>
        [JsonProperty("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }

        /// <summary>True if this achievement was created with a test API key.</summary>
        [JsonProperty("test_mode")]
        public bool TestMode { get; set; }
    }
}
