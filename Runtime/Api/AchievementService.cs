// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System.Collections.Generic;
using System.Threading.Tasks;
using Ovation.Models;

namespace Ovation.Api
{
    /// <summary>
    /// Handles achievement-related API operations: listing achievements
    /// and fetching individual achievements by slug.
    /// </summary>
    internal class AchievementService
    {
        private readonly OvationApiClient _client;

        internal AchievementService(OvationApiClient client)
        {
            _client = client;
        }

        internal async Task<ApiResult<List<Achievement>>> GetAchievementsAsync()
        {
            return await _client.GetAllPagesAsync<Achievement>("/achievements");
        }

        internal async Task<ApiResult<Achievement>> GetAchievementAsync(string slug)
        {
            return await _client.GetAsync<Achievement>($"/achievements/{slug}");
        }
    }
}
