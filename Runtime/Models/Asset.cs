// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System;
using Newtonsoft.Json;

namespace Ovation.Models
{
    /// <summary>
    /// A digital asset (image or text) bound to a specific slot. Assets are versioned —
    /// uploading a new file creates a new version while preserving old ones.
    /// Use <see cref="Url"/> to download image assets and <see cref="TextContent"/> for text-type assets.
    /// </summary>
    [Serializable]
    public class Asset
    {
        /// <summary>Unique identifier (UUID) for this asset.</summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>Asset type derived from the slot: "image", "text", or "audio".</summary>
        [JsonProperty("asset_type")]
        public string AssetType { get; set; }

        /// <summary>UUID of the slot this asset belongs to.</summary>
        [JsonProperty("slot_id")]
        public string SlotId { get; set; }

        /// <summary>Name of the slot (e.g., "badge", "spray", "emoji").</summary>
        [JsonProperty("slot_name")]
        public string SlotName { get; set; }

        /// <summary>Human-readable name for this asset.</summary>
        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        /// <summary>Optional credit line (e.g., "Art by Jane Doe").</summary>
        [JsonProperty("authority_attribution")]
        public string AuthorityAttribution { get; set; }

        /// <summary>Current version number. Increments when a new file is uploaded.</summary>
        [JsonProperty("current_version")]
        public int CurrentVersion { get; set; }

        /// <summary>Public URL to download the current version of this asset. Null for text-type assets.</summary>
        [JsonProperty("url")]
        public string Url { get; set; }

        /// <summary>Text content for text-type slots (e.g., nameplate titles). Null for image/audio assets.</summary>
        [JsonProperty("text_content")]
        public string TextContent { get; set; }

        /// <summary>When this asset was created.</summary>
        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>When this asset was last updated.</summary>
        [JsonProperty("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
