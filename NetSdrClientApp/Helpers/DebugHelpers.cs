using System;
using System.Linq;

namespace NetSdrClientApp.Helpers
{
    internal static class DebugHelpers
    {
        public static string ToHexString(byte[]? data)
        {
            if (data == null || data.Length == 0) return string.Empty;

            // Use a simple, safe format for bytes (lowercase hex, two digits)
            return string.Join(' ', data.Select(b => b.ToString("x2")));
        }
    }
}
