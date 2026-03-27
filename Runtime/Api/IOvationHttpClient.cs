// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System.Collections.Generic;
using System.Threading.Tasks;
using Ovation.Models;

namespace Ovation.Api
{
    internal interface IOvationHttpClient
    {
        void SetApiKey(string apiKey);
        Task<ApiResult<T>> GetAsync<T>(string path, Dictionary<string, string> queryParams = null, bool requiresAuth = true);
        Task<ApiResult<T>> PostAsync<T>(string path, object body = null, Dictionary<string, string> headers = null);
        Task<ApiResult<T>> PutAsync<T>(string path, object body);
        Task<ApiResult<string>> DeleteAsync(string path);
        Task<byte[]> DownloadBytesAsync(string url);
    }

    internal static class OvationHttpClientExtensions
    {
        internal static async Task<ApiResult<List<T>>> GetAllPagesAsync<T>(this IOvationHttpClient client, string path, int pageSize = 50)
        {
            var allItems = new List<T>();
            string cursor = null;

            while (true)
            {
                var queryParams = new Dictionary<string, string> { { "limit", pageSize.ToString() } };
                if (cursor != null)
                    queryParams["cursor"] = cursor;

                var result = await client.GetAsync<PaginatedResponse<T>>(path, queryParams);
                if (!result.Success)
                    return ApiResult<List<T>>.Failure(result.Error);

                allItems.AddRange(result.Data.Data);

                if (string.IsNullOrEmpty(result.Data.NextCursor))
                    break;

                cursor = result.Data.NextCursor;
            }

            return ApiResult<List<T>>.Ok(allItems);
        }
    }
}
