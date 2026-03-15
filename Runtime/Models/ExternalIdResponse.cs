// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System;
using Newtonsoft.Json;

namespace Ovation.Models
{
    /// <summary>
    /// Response from setting a player's external ID. External IDs are platform-specific
    /// identifiers (e.g., Steam ID, Xbox gamertag) scoped to a single authority.
    /// </summary>
    [Serializable]
    public class ExternalIdResponse
    {
        /// <summary>UUID of the player.</summary>
        [JsonProperty("player_id")]
        public string PlayerId { get; set; }

        /// <summary>UUID of the authority this external ID is scoped to.</summary>
        [JsonProperty("authority_id")]
        public string AuthorityId { get; set; }

        /// <summary>The external ID that was set.</summary>
        [JsonProperty("external_id")]
        public string ExternalId { get; set; }
    }
}
