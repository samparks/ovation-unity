// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System.Collections.Generic;
using System.Threading.Tasks;
using Ovation.Models;

namespace Ovation.Api
{
    /// <summary>
    /// Handles slot and equipment API operations: listing standard slots,
    /// equipping/unequipping assets, and querying equipped state.
    /// </summary>
    internal class SlotService
    {
        private readonly IOvationHttpClient _client;

        internal SlotService(IOvationHttpClient client)
        {
            _client = client;
        }

        internal async Task<ApiResult<List<Slot>>> GetStandardSlotsAsync()
        {
            return await _client.GetAsync<List<Slot>>("/slots/standard", requiresAuth: false);
        }

        internal async Task<ApiResult<List<EquippedSlotResponse>>> GetEquippedAssetsAsync(string playerId)
        {
            var queryParams = new Dictionary<string, string> { { "player_id", playerId } };
            return await _client.GetAsync<List<EquippedSlotResponse>>("/slots/equipped", queryParams);
        }

        internal async Task<ApiResult<EquippedSlotResponse>> EquipAssetAsync(string slotId, string playerId, string assetId)
        {
            var body = new { player_id = playerId, asset_id = assetId };
            return await _client.PostAsync<EquippedSlotResponse>($"/slots/{slotId}/equip", body);
        }

        internal async Task<ApiResult<string>> UnequipAssetAsync(string slotId, string playerId)
        {
            var body = new { player_id = playerId };
            return await _client.PostAsync<string>($"/slots/{slotId}/unequip", body);
        }

        internal async Task<ApiResult<EquippedSlotResponse>> GetEquippedAssetAsync(string slotName, string playerId)
        {
            var queryParams = new Dictionary<string, string> { { "player_id", playerId } };
            return await _client.GetAsync<EquippedSlotResponse>($"/slots/{slotName}/equipped/player", queryParams);
        }
    }
}
