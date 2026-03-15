// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System;
using Newtonsoft.Json;

namespace Ovation.Queue
{
    /// <summary>
    /// A serializable representation of a queued IssueAchievement request,
    /// persisted to disk for offline retry.
    /// </summary>
    [Serializable]
    internal class QueuedRequest
    {
        [JsonProperty("slug")]
        public string Slug { get; set; }

        [JsonProperty("player_id")]
        public string PlayerId { get; set; }

        [JsonProperty("idempotency_key")]
        public string IdempotencyKey { get; set; }

        [JsonProperty("queued_at_utc")]
        public DateTime QueuedAtUtc { get; set; }

        [JsonProperty("attempt_count")]
        public int AttemptCount { get; set; }

        [JsonProperty("next_retry_utc")]
        public DateTime NextRetryUtc { get; set; }
    }
}
