using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Ovation.Api;
using Ovation.Models;

namespace Ovation.Tests.Integration.Helpers
{
    /// <summary>
    /// IOvationHttpClient implementation using System.Net.Http.HttpClient.
    /// Used for integration tests that run outside of Unity.
    /// Replicates the same URL building, auth, and response parsing as OvationApiClient.
    /// </summary>
    internal class StandardHttpClient : IOvationHttpClient
    {
        private readonly string _baseUrl;
        private readonly HttpClient _http;
        private string _apiKey;

        internal StandardHttpClient(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _http = new HttpClient();
        }

        public void SetApiKey(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<ApiResult<T>> GetAsync<T>(string path, Dictionary<string, string> queryParams = null, bool requiresAuth = true)
        {
            var url = BuildUrl(path, queryParams);
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (requiresAuth) ApplyAuth(request);

            return await SendAsync<T>(request);
        }

        public async Task<ApiResult<T>> PostAsync<T>(string path, object body = null, Dictionary<string, string> headers = null)
        {
            var url = BuildUrl(path);
            var json = body != null ? JsonConvert.SerializeObject(body) : "{}";
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            ApplyAuth(request);

            if (headers != null)
            {
                foreach (var kvp in headers)
                    request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
            }

            return await SendAsync<T>(request);
        }

        public async Task<ApiResult<T>> PutAsync<T>(string path, object body)
        {
            var url = BuildUrl(path);
            var json = JsonConvert.SerializeObject(body);
            var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            ApplyAuth(request);

            return await SendAsync<T>(request);
        }

        public async Task<ApiResult<string>> DeleteAsync(string path)
        {
            var url = BuildUrl(path);
            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            ApplyAuth(request);

            return await SendAsync<string>(request);
        }

        public async Task<byte[]> DownloadBytesAsync(string url)
        {
            try
            {
                return await _http.GetByteArrayAsync(url);
            }
            catch
            {
                return null;
            }
        }

        private async Task<ApiResult<T>> SendAsync<T>(HttpRequestMessage request)
        {
            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request);
            }
            catch (HttpRequestException ex)
            {
                return ApiResult<T>.Failure(new OvationError
                {
                    Code = "network_error",
                    Message = ex.Message,
                    IsNetworkError = true,
                    HttpStatusCode = 0
                });
            }

            var statusCode = (int)response.StatusCode;
            var responseBody = await response.Content.ReadAsStringAsync();

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
                    return ApiResult<T>.Failure(new OvationError
                    {
                        Code = "parse_error",
                        Message = $"Failed to parse response: {ex.Message}",
                        HttpStatusCode = statusCode
                    });
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
                    Message = responseBody
                };
            }

            apiError.HttpStatusCode = statusCode;
            apiError.IsNetworkError = false;
            return ApiResult<T>.Failure(apiError);
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
                    sb.Append(Uri.EscapeDataString(kvp.Key));
                    sb.Append('=');
                    sb.Append(Uri.EscapeDataString(kvp.Value));
                    first = false;
                }
            }

            return sb.ToString();
        }

        private void ApplyAuth(HttpRequestMessage request)
        {
            if (!string.IsNullOrEmpty(_apiKey))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        }
    }
}
