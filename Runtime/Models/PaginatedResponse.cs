// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Ovation.Models
{
    /// <summary>
    /// Cursor-based paginated response from the Ovation API. When <see cref="NextCursor"/>
    /// is null, you have reached the last page. The SDK auto-paginates for you in most cases,
    /// so you typically won't interact with this class directly.
    /// </summary>
    [System.Serializable]
    public class PaginatedResponse<T>
    {
        /// <summary>Items on this page.</summary>
        [JsonProperty("data")]
        public List<T> Data { get; set; } = new List<T>();

        /// <summary>Cursor to fetch the next page, or null if this is the last page.</summary>
        [JsonProperty("next_cursor")]
        public string NextCursor { get; set; }
    }
}
