// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System;
using Newtonsoft.Json;

namespace Ovation.Models
{
    /// <summary>
    /// An authority (game studio, brand, or platform) that issues achievements.
    /// This is the top-level tenant — all achievements, assets, and players are scoped to an authority.
    /// </summary>
    [Serializable]
    public class Authority
    {
        /// <summary>Unique identifier (UUID) for this authority.</summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>Display name of the authority (e.g., "Cool Game Studio").</summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>Authority type: "game_studio", "brand", "business", "platform", "event", or "web_app".</summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>Authority's website URL, if provided.</summary>
        [JsonProperty("website")]
        public string Website { get; set; }

        /// <summary>Whether this authority has been verified by Ovation admins.</summary>
        [JsonProperty("verified")]
        public bool Verified { get; set; }

        /// <summary>When this authority was created.</summary>
        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }
    }
}
