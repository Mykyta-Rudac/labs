using System;

namespace EchoTcpServer
{
    internal static class LogHelper
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
