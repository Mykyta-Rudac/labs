using System;

namespace Common.Helpers
{
    /// <summary>
    /// Shared logging helper for console output.
    /// Used by both EchoTcpServer and NetSdrClientApp to avoid code duplication.
    /// </summary>
    public static class LogHelper
    {
        public static void Log(string message)
        {
            Console.WriteLine(message);
        }

        public static void LogSocketError(string context, Exception ex)
        {
            Console.WriteLine($"{context}: {ex.Message}");
        }
    }
}
