// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System;
using Newtonsoft.Json;

namespace Ovation.Models
{
    /// <summary>
    /// Compact asset information returned as part of player achievement data.
    /// Contains enough info to display or download the asset without a separate API call.
    /// For full asset details, use <see cref="OvationSDK.GetAssetAsync"/>.
    /// </summary>
    [Serializable]
    public class AssetSummary
    {
        /// <summary>Unique identifier (UUID) for this asset.</summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>UUID of the slot this asset belongs to.</summary>
        [JsonProperty("slot_id")]
        public string SlotId { get; set; }

        /// <summary>Name of the slot (e.g., "badge", "spray", "emoji").</summary>
        [JsonProperty("slot_name")]
        public string SlotName { get; set; }

        /// <summary>Public URL to download this asset image.</summary>
        [JsonProperty("url")]
        public string Url { get; set; }

        /// <summary>Version number of this asset.</summary>
        [JsonProperty("version")]
        public int Version { get; set; }

        /// <summary>Human-readable name for this asset.</summary>
        [JsonProperty("display_name")]
        public string DisplayName { get; set; }
    }
}
