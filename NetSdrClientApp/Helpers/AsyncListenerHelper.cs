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
        /// <summary>
        /// Handle common listener exceptions.
        /// Returns true if the caller should rethrow the exception (used for UDP where SocketException is rethrown).
        /// </summary>
        public static bool HandleListenerException(Exception ex, string context, bool rethrowSocket = false)
        {
            switch (ex)
            {
                case OperationCanceledException:
                    // Operation was cancelled - this is normal shutdown
                    return false;
                case IOException ioEx:
                    LogHelper.Log($"I/O error {context}: {ioEx.Message}");
                    return false;
                case SocketException sockEx:
                    LogHelper.LogSocketError($"Socket error {context}", sockEx);
                    return rethrowSocket;
                case ObjectDisposedException _:
                    // Socket or resource disposed during shutdown
                    return false;
                default:
                    LogHelper.Log($"Unexpected error {context}: {ex.Message}");
                    return false;
            }
        }
    }
}
