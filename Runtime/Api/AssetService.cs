// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System.Threading.Tasks;
using Ovation.Models;

namespace Ovation.Api
{
    /// <summary>
    /// Handles read-only asset API operations. Asset uploading and management
    /// is done through the authority portal, not the game SDK.
    /// </summary>
    internal class AssetService
    {
        private readonly IOvationHttpClient _client;

        internal AssetService(IOvationHttpClient client)
        {
            _client = client;
        }

        internal async Task<ApiResult<Asset>> GetAssetAsync(string assetId)
        {
            return await _client.GetAsync<Asset>($"/assets/{assetId}");
        }
    }
}
