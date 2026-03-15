// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System;

namespace Ovation.Utils
{
    /// <summary>
    /// Generates unique idempotency keys for API requests.
    /// Idempotency keys prevent duplicate achievement issuance on retry.
    /// </summary>
    internal static class IdempotencyKeyGenerator
    {
        internal static string Generate()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
