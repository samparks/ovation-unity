// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using UnityEngine;

namespace Ovation.Utils
{
    /// <summary>
    /// Internal logging utility. All messages are prefixed with [Ovation].
    /// Log and Warning are gated behind the debug logging setting.
    /// Errors always log regardless of the setting.
    /// </summary>
    internal static class OvationLogger
    {
        private static bool _enabled;

        internal static void SetEnabled(bool enabled)
        {
            _enabled = enabled;
        }

        internal static void Log(string message)
        {
            if (_enabled)
                Debug.Log($"[Ovation] {message}");
        }

        internal static void Warning(string message)
        {
            if (_enabled)
                Debug.LogWarning($"[Ovation] WARNING: {message}");
        }

        internal static void Error(string message)
        {
            Debug.LogError($"[Ovation] ERROR: {message}");
        }
    }
}
