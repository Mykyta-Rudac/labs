using System;
using System.Linq;

namespace NetSdrClientApp.Helpers
{
    internal static class DebugHelpers
    {
        public static string ToHexString(byte[] data)
        {
            if (data == null || data.Length == 0) return string.Empty;
            try
            {
                return string.Join(' ', data.Select(b => Convert.ToString(b, 16).PadLeft(2, '0')));
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
