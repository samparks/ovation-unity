// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System;
using Newtonsoft.Json;

namespace Ovation.Models
{
    /// <summary>
    /// Represents an error from the Ovation API or a network failure.
    /// Check <see cref="IsNetworkError"/> to distinguish between API errors (the server responded
    /// with an error code) and network errors (the request never reached the server).
    ///
    /// Common API error codes:
    /// - "authentication_failed" (401) — missing, invalid, or inactive API key
    /// - "achievement_not_found" (404) — slug doesn't exist for this authority
    /// - "player_not_found" (404) — player ID doesn't exist
    /// - "achievement_archived" (410) — achievement has been soft-deleted
    /// - "slug_already_exists" (409) — duplicate achievement slug
    /// - "external_id_conflict" (409) — external ID linked to a different player
    /// </summary>
    [Serializable]
    public class OvationError
    {
        /// <summary>Machine-readable error code (e.g., "achievement_not_found"). Use this for programmatic error handling.</summary>
        [JsonProperty("code")]
        public string Code { get; set; }

        /// <summary>Human-readable error message suitable for logging.</summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>HTTP status code from the API response (e.g., 404, 401). Zero for network errors.</summary>
        [JsonIgnore]
        public int HttpStatusCode { get; set; }

        /// <summary>True if the request failed due to a network error (timeout, no connectivity, DNS failure) rather than an API error.</summary>
        [JsonIgnore]
        public bool IsNetworkError { get; set; }

        public override string ToString()
        {
            if (IsNetworkError)
                return $"[Ovation] Network error: {Message}";
            return $"[Ovation] API error ({HttpStatusCode}): {Code} - {Message}";
        }
    }

    /// <summary>
    /// Internal wrapper for the API error response envelope: { "error": { ... } }.
    /// </summary>
    [Serializable]
    internal class ErrorResponse
    {
        [JsonProperty("error")]
        public OvationError Error { get; set; }
    }
}
