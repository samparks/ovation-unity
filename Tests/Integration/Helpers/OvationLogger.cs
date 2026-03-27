// Stub for OvationLogger — replaces Unity's Debug.Log with Console.WriteLine for tests.
// Must match the same namespace and method signatures as the real OvationLogger.

namespace Ovation.Utils
{
    internal static class OvationLogger
    {
        private static bool _enabled = false;

        internal static void SetEnabled(bool enabled) => _enabled = enabled;

        internal static void Log(string message)
        {
            if (_enabled)
                System.Console.WriteLine($"[Ovation] {message}");
        }

        internal static void Warning(string message)
        {
            if (_enabled)
                System.Console.WriteLine($"[Ovation] WARNING: {message}");
        }

        internal static void Error(string message)
        {
            if (_enabled)
                System.Console.Error.WriteLine($"[Ovation] ERROR: {message}");
        }
    }
}
