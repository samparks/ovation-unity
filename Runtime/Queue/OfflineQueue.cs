// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Ovation.Api;
using Ovation.Models;
using Ovation.Utils;
using UnityEngine;

namespace Ovation.Queue
{
    /// <summary>
    /// Persists failed IssueAchievement requests to disk and retries them when connectivity returns.
    /// Queue is saved to Application.persistentDataPath/OvationQueue/queue.json and survives app restarts.
    /// Uses idempotency keys to prevent duplicate issuance. Retries up to 5 times with exponential backoff.
    /// Capped at a configurable max size (default 100) — oldest items are dropped when full.
    /// </summary>
    internal class OfflineQueue
    {
        private const int MaxRetries = 5;
        private static readonly double[] RetryDelaysSeconds = { 0, 60, 300, 1800, 7200 };

        private readonly PlayerService _playerService;
        private readonly int _maxQueueSize;
        private readonly string _queueDir;
        private readonly string _queuePath;
        private List<QueuedRequest> _queue;

        internal event Action<string, IssueAchievementResult> OnQueuedAchievementSynced;
        internal int Count => _queue?.Count ?? 0;

        internal OfflineQueue(PlayerService playerService, int maxQueueSize)
        {
            _playerService = playerService;
            _maxQueueSize = maxQueueSize;
            _queueDir = Path.Combine(Application.persistentDataPath, "OvationQueue");
            _queuePath = Path.Combine(_queueDir, "queue.json");
            Load();
        }

        internal void Enqueue(string slug, string playerId, string idempotencyKey)
        {
            if (_queue.Count >= _maxQueueSize)
            {
                // Drop oldest
                OvationLogger.Warning($"Offline queue full ({_maxQueueSize}). Dropping oldest entry.");
                _queue.RemoveAt(0);
            }

            _queue.Add(new QueuedRequest
            {
                Slug = slug,
                PlayerId = playerId,
                IdempotencyKey = idempotencyKey ?? IdempotencyKeyGenerator.Generate(),
                QueuedAtUtc = DateTime.UtcNow,
                AttemptCount = 0,
                NextRetryUtc = DateTime.UtcNow
            });

            Save();
            OvationLogger.Log($"Achievement '{slug}' queued for offline sync");
        }

        internal async Task FlushAsync()
        {
            if (_queue.Count == 0)
                return;

            var now = DateTime.UtcNow;
            var toProcess = _queue.Where(r => r.NextRetryUtc <= now).ToList();

            if (toProcess.Count == 0)
                return;

            OvationLogger.Log($"Flushing offline queue: {toProcess.Count} items to sync");

            var toRemove = new List<QueuedRequest>();

            foreach (var request in toProcess)
            {
                var result = await _playerService.IssueAchievementAsync(
                    request.PlayerId, request.Slug, request.IdempotencyKey);

                if (result.Success)
                {
                    OvationLogger.Log($"Queued achievement synced: {request.Slug}");
                    toRemove.Add(request);

                    try
                    {
                        OnQueuedAchievementSynced?.Invoke(request.Slug, result.Data);
                    }
                    catch (Exception ex)
                    {
                        OvationLogger.Warning($"Error in OnQueuedAchievementSynced handler: {ex.Message}");
                    }
                }
                else if (result.Error.IsNetworkError)
                {
                    // Still offline, stop trying for now
                    OvationLogger.Log("Still offline, stopping queue flush");
                    break;
                }
                else
                {
                    // API error (4xx) — increment attempt counter
                    request.AttemptCount++;

                    if (request.AttemptCount >= MaxRetries)
                    {
                        OvationLogger.Warning($"Queued achievement '{request.Slug}' failed after {MaxRetries} attempts. Dropping.");
                        toRemove.Add(request);
                    }
                    else
                    {
                        var delayIndex = Math.Min(request.AttemptCount, RetryDelaysSeconds.Length - 1);
                        request.NextRetryUtc = DateTime.UtcNow.AddSeconds(RetryDelaysSeconds[delayIndex]);
                        OvationLogger.Warning($"Queued achievement '{request.Slug}' failed (attempt {request.AttemptCount}). Retrying at {request.NextRetryUtc:u}");
                    }
                }
            }

            foreach (var item in toRemove)
                _queue.Remove(item);

            Save();
        }

        private void Load()
        {
            if (File.Exists(_queuePath))
            {
                try
                {
                    var json = File.ReadAllText(_queuePath);
                    _queue = JsonConvert.DeserializeObject<List<QueuedRequest>>(json) ?? new List<QueuedRequest>();
                    if (_queue.Count > 0)
                        OvationLogger.Log($"Loaded {_queue.Count} queued items from disk");
                    return;
                }
                catch (Exception ex)
                {
                    OvationLogger.Warning($"Failed to load offline queue: {ex.Message}");
                }
            }

            _queue = new List<QueuedRequest>();
        }

        private void Save()
        {
            try
            {
                if (!Directory.Exists(_queueDir))
                    Directory.CreateDirectory(_queueDir);

                var json = JsonConvert.SerializeObject(_queue, Formatting.Indented);
                File.WriteAllText(_queuePath, json);
            }
            catch (Exception ex)
            {
                OvationLogger.Warning($"Failed to save offline queue: {ex.Message}");
            }
        }
    }
}
