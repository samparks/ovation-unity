// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Ovation.Models
{
    /// <summary>
    /// Represents a player in the Ovation system. Players start as anonymous (just a UUID)
    /// and can optionally be linked to platform-specific external IDs (Steam, Xbox, etc.).
    /// </summary>
    [Serializable]
    public class Player
    {
        /// <summary>Unique Ovation player identifier (UUID).</summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>True if the player has not yet been linked to an Ovation account (Phase 2).</summary>
        [JsonProperty("anonymous")]
        public bool Anonymous { get; set; }

        /// <summary>Platform-specific external ID for this authority (e.g., Steam ID), or null if not set.</summary>
        [JsonProperty("external_id")]
        public string ExternalId { get; set; }

        /// <summary>Achievements earned by this player. May be empty if not requested or none earned.</summary>
        [JsonProperty("achievements")]
        public List<PlayerAchievement> Achievements { get; set; } = new List<PlayerAchievement>();

        /// <summary>When this player record was created.</summary>
        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }
    }
}
