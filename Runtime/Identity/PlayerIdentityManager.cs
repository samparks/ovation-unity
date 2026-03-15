// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System.Threading.Tasks;
using Ovation.Api;
using Ovation.Utils;
using UnityEngine;

namespace Ovation.Identity
{
    /// <summary>
    /// Manages automatic player identity. On first use, creates an anonymous player via the API
    /// and stores the UUID in PlayerPrefs. On subsequent sessions, reuses the stored ID.
    /// Can be disabled via OvationConfig.AutoManagePlayerId for games that manage IDs themselves.
    /// </summary>
    internal class PlayerIdentityManager
    {
        private const string PlayerIdKey = "Ovation_PlayerId";

        private readonly PlayerService _playerService;
        private string _playerId;

        internal string PlayerId => _playerId;
        internal bool HasPlayerId => !string.IsNullOrEmpty(_playerId);

        internal PlayerIdentityManager(PlayerService playerService)
        {
            _playerService = playerService;
        }

        internal async Task<bool> EnsurePlayerIdAsync()
        {
            // Check if already loaded
            if (HasPlayerId)
                return true;

            // Try loading from PlayerPrefs
            _playerId = PlayerPrefs.GetString(PlayerIdKey, null);
            if (!string.IsNullOrEmpty(_playerId))
            {
                OvationLogger.Log($"Loaded player ID from storage: {_playerId}");
                return true;
            }

            // Create new anonymous player
            var result = await _playerService.CreatePlayerAsync();
            if (result.Success)
            {
                _playerId = result.Data.Id;
                PlayerPrefs.SetString(PlayerIdKey, _playerId);
                PlayerPrefs.Save();
                OvationLogger.Log($"Created and stored new player ID: {_playerId}");
                return true;
            }

            OvationLogger.Error($"Failed to create player: {result.Error}");
            return false;
        }

        /// <summary>
        /// Set a known player ID directly. Use this when managing player IDs yourself.
        /// </summary>
        internal void SetPlayerId(string playerId)
        {
            _playerId = playerId;
            PlayerPrefs.SetString(PlayerIdKey, playerId);
            PlayerPrefs.Save();
            OvationLogger.Log($"Player ID set manually: {playerId}");
        }

        /// <summary>
        /// Clear the stored player ID. Next initialization will create a new one.
        /// </summary>
        internal void ClearPlayerId()
        {
            _playerId = null;
            PlayerPrefs.DeleteKey(PlayerIdKey);
            PlayerPrefs.Save();
            OvationLogger.Log("Player ID cleared");
        }
    }
}
