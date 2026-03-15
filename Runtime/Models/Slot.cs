// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Ovation.Models
{
    /// <summary>
    /// A global customization point defined by Ovation. Slots define where and how assets
    /// can be displayed in a game (e.g., "badge", "spray", "avatar_frame"). Each slot carries
    /// full specifications for asset creation: required dimensions, file formats, max file size,
    /// and transparency rules.
    /// </summary>
    [Serializable]
    public class Slot
    {
        /// <summary>Unique identifier (UUID) for this slot.</summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>Immutable slot identifier (e.g., "badge", "avatar_frame", "spray").</summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>Human-readable name (e.g., "Avatar Frame").</summary>
        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        /// <summary>Description of what this slot is for.</summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>Type of assets this slot accepts: "image", "text", or "audio".</summary>
        [JsonProperty("asset_type")]
        public string AssetType { get; set; }

        /// <summary>Accepted file formats (e.g., ["png", "webp"]). Null for text slots.</summary>
        [JsonProperty("file_formats")]
        public List<string> FileFormats { get; set; }

        /// <summary>Required image width in pixels. Null for text/audio slots.</summary>
        [JsonProperty("width")]
        public int? Width { get; set; }

        /// <summary>Required image height in pixels. Null for text/audio slots.</summary>
        [JsonProperty("height")]
        public int? Height { get; set; }

        /// <summary>Inner cutout width for compositing slots like avatar_frame (transparent center area). Null for most slots.</summary>
        [JsonProperty("inner_width")]
        public int? InnerWidth { get; set; }

        /// <summary>Inner cutout height for compositing slots like avatar_frame. Null for most slots.</summary>
        [JsonProperty("inner_height")]
        public int? InnerHeight { get; set; }

        /// <summary>Maximum upload file size in bytes. Null for text slots.</summary>
        [JsonProperty("max_file_size_bytes")]
        public int? MaxFileSizeBytes { get; set; }

        /// <summary>Transparency requirement: "required", "optional", or "forbidden".</summary>
        [JsonProperty("transparency")]
        public string Transparency { get; set; }

        /// <summary>Whether animated assets (e.g., animated PNG) are accepted.</summary>
        [JsonProperty("animation_allowed")]
        public bool AnimationAllowed { get; set; }

        /// <summary>Maximum text length for text-type slots. Null for image/audio slots.</summary>
        [JsonProperty("text_max_length")]
        public int? TextMaxLength { get; set; }

        /// <summary>Regex pattern for allowed characters in text-type slots. Null for image/audio slots.</summary>
        [JsonProperty("text_allowed_pattern")]
        public string TextAllowedPattern { get; set; }

        /// <summary>Instructions for authorities creating assets for this slot.</summary>
        [JsonProperty("authority_guidance")]
        public string AuthorityGuidance { get; set; }

        /// <summary>Instructions for game developers implementing this slot in their game.</summary>
        [JsonProperty("implementation_notes")]
        public string ImplementationNotes { get; set; }

        /// <summary>When this slot was created.</summary>
        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>When this slot was last updated.</summary>
        [JsonProperty("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
