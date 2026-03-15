// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Ovation.Api;
using Ovation.Utils;
using UnityEngine;

namespace Ovation.Cache
{
    /// <summary>
    /// Downloads and caches asset images to disk. Cache keys are SHA256(url + version).
    /// Uses LRU eviction when the cache exceeds the configured max size.
    /// Files are stored in Application.persistentDataPath/OvationCache/ and survive app restarts.
    /// </summary>
    internal class AssetCache
    {
        private readonly OvationApiClient _client;
        private readonly string _cacheDir;
        private readonly string _indexPath;
        private readonly long _maxCacheBytes;
        private CacheIndex _index;

        internal AssetCache(OvationApiClient client, int maxCacheSizeMB)
        {
            _client = client;
            _cacheDir = Path.Combine(Application.persistentDataPath, "OvationCache");
            _indexPath = Path.Combine(_cacheDir, "cache_index.json");
            _maxCacheBytes = (long)maxCacheSizeMB * 1024 * 1024;
            LoadIndex();
        }

        internal async Task<Texture2D> LoadTextureAsync(string url, int version)
        {
            var bytes = await GetOrDownloadAsync(url, version);
            if (bytes == null) return null;

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (texture.LoadImage(bytes))
                return texture;

            UnityEngine.Object.Destroy(texture);
            return null;
        }

        internal async Task<Sprite> LoadSpriteAsync(string url, int version)
        {
            var texture = await LoadTextureAsync(url, version);
            if (texture == null) return null;

            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
        }

        private async Task<byte[]> GetOrDownloadAsync(string url, int version)
        {
            var cacheKey = ComputeCacheKey(url, version);
            var filePath = Path.Combine(_cacheDir, cacheKey);

            // Check cache
            if (_index.Entries.TryGetValue(cacheKey, out var entry) && entry.Version == version && File.Exists(filePath))
            {
                entry.LastAccessedUtc = DateTime.UtcNow;
                SaveIndex();
                OvationLogger.Log($"Asset loaded from cache: {url}");
                return File.ReadAllBytes(filePath);
            }

            // Download
            var bytes = await _client.DownloadBytesAsync(url);
            if (bytes == null)
            {
                OvationLogger.Warning($"Failed to download asset: {url}");
                return null;
            }

            // Ensure directory exists
            if (!Directory.Exists(_cacheDir))
                Directory.CreateDirectory(_cacheDir);

            // Write to disk
            File.WriteAllBytes(filePath, bytes);

            // Update index
            _index.Entries[cacheKey] = new CacheEntry
            {
                Url = url,
                Version = version,
                FileSizeBytes = bytes.Length,
                LastAccessedUtc = DateTime.UtcNow
            };
            SaveIndex();

            // Evict if over limit
            EnforceSizeLimit();

            OvationLogger.Log($"Asset downloaded and cached: {url} (v{version})");
            return bytes;
        }

        private void EnforceSizeLimit()
        {
            var totalSize = _index.Entries.Values.Sum(e => (long)e.FileSizeBytes);

            if (totalSize <= _maxCacheBytes)
                return;

            // LRU eviction: sort by last accessed, remove oldest first
            var sorted = _index.Entries
                .OrderBy(kvp => kvp.Value.LastAccessedUtc)
                .ToList();

            foreach (var kvp in sorted)
            {
                if (totalSize <= _maxCacheBytes)
                    break;

                var filePath = Path.Combine(_cacheDir, kvp.Key);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    OvationLogger.Log($"Evicted cached asset: {kvp.Value.Url}");
                }

                totalSize -= kvp.Value.FileSizeBytes;
                _index.Entries.Remove(kvp.Key);
            }

            SaveIndex();
        }

        private static string ComputeCacheKey(string url, int version)
        {
            using var sha = SHA256.Create();
            var input = $"{url}|v{version}";
            var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        private void LoadIndex()
        {
            if (File.Exists(_indexPath))
            {
                try
                {
                    var json = File.ReadAllText(_indexPath);
                    _index = JsonConvert.DeserializeObject<CacheIndex>(json) ?? new CacheIndex();
                    return;
                }
                catch (Exception ex)
                {
                    OvationLogger.Warning($"Failed to load cache index, starting fresh: {ex.Message}");
                }
            }

            _index = new CacheIndex();
        }

        private void SaveIndex()
        {
            try
            {
                if (!Directory.Exists(_cacheDir))
                    Directory.CreateDirectory(_cacheDir);

                var json = JsonConvert.SerializeObject(_index, Formatting.Indented);
                File.WriteAllText(_indexPath, json);
            }
            catch (Exception ex)
            {
                OvationLogger.Warning($"Failed to save cache index: {ex.Message}");
            }
        }
    }

    [Serializable]
    internal class CacheIndex
    {
        [JsonProperty("entries")]
        public Dictionary<string, CacheEntry> Entries { get; set; } = new Dictionary<string, CacheEntry>();
    }

    [Serializable]
    internal class CacheEntry
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("file_size_bytes")]
        public int FileSizeBytes { get; set; }

        [JsonProperty("last_accessed_utc")]
        public DateTime LastAccessedUtc { get; set; }
    }
}
