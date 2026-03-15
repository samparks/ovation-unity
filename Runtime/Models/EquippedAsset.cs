// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System;
using Newtonsoft.Json;

namespace Ovation.Models
{
    /// <summary>
    /// An asset that a player has equipped in a slot. Contains the asset's download URL,
    /// version, and the achievement it came from.
    /// </summary>
    [Serializable]
    public class EquippedAsset
    {
        /// <summary>Unique identifier (UUID) for this asset.</summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>Asset type: "image", "text", or "audio".</summary>
        [JsonProperty("asset_type")]
        public string AssetType { get; set; }

        /// <summary>Public URL to download this asset image.</summary>
        [JsonProperty("url")]
        public string Url { get; set; }

        /// <summary>Current version number of this asset.</summary>
        [JsonProperty("version")]
        public int Version { get; set; }

        /// <summary>Human-readable name for this asset.</summary>
        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        /// <summary>Slug of the achievement this asset is bound to.</summary>
        [JsonProperty("achievement_slug")]
        public string AchievementSlug { get; set; }

        /// <summary>Name of the authority (game studio) that created this asset.</summary>
        [JsonProperty("authority_name")]
        public string AuthorityName { get; set; }
    }

    /// <summary>
    /// Response from equipped asset endpoints. Wraps an <see cref="EquippedAsset"/>
    /// with the slot name and player ID for context.
    /// </summary>
    [Serializable]
    public class EquippedSlotResponse
    {
        /// <summary>Name of the slot (e.g., "badge", "avatar_frame").</summary>
        [JsonProperty("slot")]
        public string Slot { get; set; }

        /// <summary>UUID of the player this equipment belongs to.</summary>
        [JsonProperty("player_id")]
        public string PlayerId { get; set; }

        /// <summary>The equipped asset, or null if nothing is equipped in this slot.</summary>
        [JsonProperty("equipped_asset")]
        public EquippedAsset EquippedAsset { get; set; }
    }
}
