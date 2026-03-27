// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Ovation.Models;
using Ovation.Utils;
using UnityEngine;
using UnityEngine.Networking;

namespace Ovation.Api
{
    /// <summary>
    /// Core HTTP client built on UnityWebRequest. Handles base URL construction,
    /// Bearer token injection, JSON serialization (via Newtonsoft), error response
    /// parsing, and auto-pagination. All requests run on the main thread via Unity's async context.
    /// </summary>
    internal class OvationApiClient : IOvationHttpClient
    {
        private readonly string _baseUrl;
        private string _apiKey;

        internal OvationApiClient(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
        }

        internal void SetApiKey(string apiKey)
        {
            _apiKey = apiKey;
        }

        internal async Task<ApiResult<T>> GetAsync<T>(string path, Dictionary<string, string> queryParams = null, bool requiresAuth = true)
        {
            var url = BuildUrl(path, queryParams);
            using var request = UnityWebRequest.Get(url);
            ApplyHeaders(request, requiresAuth);

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            return ParseResponse<T>(request);
        }

        internal async Task<ApiResult<T>> PostAsync<T>(string path, object body = null, Dictionary<string, string> headers = null)
        {
            var url = BuildUrl(path);
            var json = body != null ? JsonConvert.SerializeObject(body) : "{}";
            using var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            ApplyHeaders(request, true);
            request.SetRequestHeader("Content-Type", "application/json");

            if (headers != null)
            {
                foreach (var kvp in headers)
                    request.SetRequestHeader(kvp.Key, kvp.Value);
            }

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            return ParseResponse<T>(request);
        }

        internal async Task<ApiResult<T>> PutAsync<T>(string path, object body)
        {
            var url = BuildUrl(path);
            var json = JsonConvert.SerializeObject(body);
            using var request = new UnityWebRequest(url, "PUT");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            ApplyHeaders(request, true);
            request.SetRequestHeader("Content-Type", "application/json");

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            return ParseResponse<T>(request);
        }

        internal async Task<ApiResult<string>> DeleteAsync(string path)
        {
            var url = BuildUrl(path);
            using var request = UnityWebRequest.Delete(url);
            request.downloadHandler = new DownloadHandlerBuffer();
            ApplyHeaders(request, true);

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            return ParseResponse<string>(request);
        }

        internal async Task<byte[]> DownloadBytesAsync(string url)
        {
            using var request = UnityWebRequest.Get(url);
            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
                return null;

            return request.downloadHandler.data;
        }

        private string BuildUrl(string path, Dictionary<string, string> queryParams = null)
        {
            var sb = new StringBuilder($"{_baseUrl}/v1{path}");

            if (queryParams != null && queryParams.Count > 0)
            {
                sb.Append('?');
                bool first = true;
                foreach (var kvp in queryParams)
                {
                    if (!first) sb.Append('&');
                    sb.Append(UnityWebRequest.EscapeURL(kvp.Key));
                    sb.Append('=');
                    sb.Append(UnityWebRequest.EscapeURL(kvp.Value));
                    first = false;
                }
            }

            return sb.ToString();
        }

        private void ApplyHeaders(UnityWebRequest request, bool requiresAuth)
        {
            if (requiresAuth && !string.IsNullOrEmpty(_apiKey))
                request.SetRequestHeader("Authorization", $"Bearer {_apiKey}");
        }

        private ApiResult<T> ParseResponse<T>(UnityWebRequest request)
        {
            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.DataProcessingError)
            {
                var networkError = new OvationError
                {
                    Code = "network_error",
                    Message = request.error,
                    IsNetworkError = true,
                    HttpStatusCode = 0
                };
                OvationLogger.Error($"Network error: {request.error}");
                return ApiResult<T>.Failure(networkError);
            }

            var statusCode = (int)request.responseCode;
            var responseBody = request.downloadHandler?.text;

            if (statusCode >= 200 && statusCode < 300)
            {
                try
                {
                    if (typeof(T) == typeof(string))
                        return ApiResult<T>.Ok((T)(object)(responseBody ?? ""));

                    var data = JsonConvert.DeserializeObject<T>(responseBody);
                    return ApiResult<T>.Ok(data);
                }
                catch (Exception ex)
                {
                    var parseError = new OvationError
                    {
                        Code = "parse_error",
                        Message = $"Failed to parse response: {ex.Message}",
                        HttpStatusCode = statusCode
                    };
                    OvationLogger.Error($"Parse error: {ex.Message}");
                    return ApiResult<T>.Failure(parseError);
                }
            }

            // Error response
            OvationError apiError;
            try
            {
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseBody);
                apiError = errorResponse?.Error ?? new OvationError
                {
                    Code = "unknown_error",
                    Message = responseBody
                };
            }
            catch
            {
                apiError = new OvationError
                {
                    Code = "unknown_error",
                    Message = responseBody ?? request.error
                };
            }

            apiError.HttpStatusCode = statusCode;
            apiError.IsNetworkError = false;
            OvationLogger.Error($"API call failed ({statusCode}): {apiError.Code} - {apiError.Message}");
            return ApiResult<T>.Failure(apiError);
        }
    }

    internal class ApiResult<T>
    {
        internal bool Success { get; private set; }
        internal T Data { get; private set; }
        internal OvationError Error { get; private set; }

        internal static ApiResult<T> Ok(T data) => new ApiResult<T> { Success = true, Data = data };
        internal static ApiResult<T> Failure(OvationError error) => new ApiResult<T> { Success = false, Error = error };
    }
}
