using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NetSdrClientApp.Helpers
{
    /// <summary>
    /// Helper class to consolidate exception handling for async listening operations.
    /// </summary>
    public static class AsyncListenerHelper
    {
        public static void HandleListenerException(Exception ex, string context)
        {
            switch (ex)
            {
                case OperationCanceledException:
                    // Operation was cancelled - this is normal shutdown
                    break;
                case IOException ioEx:
                    LogHelper.Log($"I/O error {context}: {ioEx.Message}");
                    break;
                case SocketException sockEx:
                    LogHelper.LogSocketError($"Socket error {context}", sockEx);
                    break;
                case ObjectDisposedException dispEx:
                    // Socket or resource disposed during shutdown
                    break;
                default:
                    LogHelper.Log($"Unexpected error {context}: {ex.Message}");
                    break;
            }
        }
    }
}
