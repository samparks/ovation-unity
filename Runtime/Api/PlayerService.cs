// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ovation.Models;
using Ovation.Utils;

namespace Ovation.Api
{
    /// <summary>
    /// Handles player-related API operations: creating anonymous players,
    /// setting external IDs, issuing achievements, and fetching player data.
    /// </summary>
    internal class PlayerService
    {
        private readonly IOvationHttpClient _client;

        internal PlayerService(IOvationHttpClient client)
        {
            _client = client;
        }

        internal async Task<ApiResult<Player>> CreatePlayerAsync()
        {
            var result = await _client.PostAsync<Player>("/players");
            if (result.Success)
                OvationLogger.Log($"Player created: {result.Data.Id}");
            return result;
        }

        internal async Task<ApiResult<Player>> GetPlayerAsync(string playerId)
        {
            return await _client.GetAsync<Player>($"/players/{playerId}");
        }

        internal async Task<ApiResult<Player>> GetPlayerByExternalIdAsync(string externalId)
        {
            return await _client.GetAsync<Player>($"/players/by-external-id/{externalId}");
        }

        internal async Task<ApiResult<ExternalIdResponse>> SetExternalIdAsync(string playerId, string externalId)
        {
            var body = new { external_id = externalId };
            var result = await _client.PutAsync<ExternalIdResponse>($"/players/{playerId}/external-id", body);
            if (result.Success)
                OvationLogger.Log($"External ID set for player {playerId}: {externalId}");
            return result;
        }

        internal async Task<ApiResult<IssueAchievementResult>> IssueAchievementAsync(
            string playerId, string slug, string idempotencyKey = null)
        {
            var body = new { slug };
            var headers = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(idempotencyKey))
                headers["Idempotency-Key"] = idempotencyKey;

            var result = await _client.PostAsync<IssueAchievementResult>(
                $"/players/{playerId}/achievements", body, headers.Count > 0 ? headers : null);

            if (result.Success)
                OvationLogger.Log($"Achievement issued: {slug} (was_new: {result.Data.WasNew})");

            return result;
        }

        internal async Task<ApiResult<List<PlayerAchievement>>> GetPlayerAchievementsAsync(string playerId)
        {
            return await _client.GetAllPagesAsync<PlayerAchievement>($"/players/{playerId}/achievements");
        }
    }
}
