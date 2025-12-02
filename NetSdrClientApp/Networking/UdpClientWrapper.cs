using System;
using System.Net;
using System.Net.Sockets;
// No cryptographic hash required for GetHashCode; use HashCode.Combine instead
using System.Threading;
using System.Threading.Tasks;
using Common.Helpers;

namespace NetSdrClientApp.Networking
{
    public class UdpClientWrapper : IUdpClient
    {
        private readonly IPEndPoint _localEndPoint;
        private CancellationTokenSource? _cts;
        private UdpClient? _udpClient;

        public event EventHandler<byte[]>? MessageReceived;

        public UdpClientWrapper(int port)
        {
            _localEndPoint = new IPEndPoint(IPAddress.Any, port);
        }

        public async Task StartListeningAsync()
        {
            _cts = new CancellationTokenSource();
            Console.WriteLine("Start listening for UDP messages...");

            try
            {
                _udpClient = new UdpClient(_localEndPoint);
                while (!_cts.Token.IsCancellationRequested)
                {
                    UdpReceiveResult result = await _udpClient.ReceiveAsync(_cts.Token);
                    MessageReceived?.Invoke(this, result.Buffer);

                    Console.WriteLine($"Received from {result.RemoteEndPoint}");
                }
            }
            catch (Exception ex)
            {
                // For UDP we want to rethrow on SocketException to let callers observe it
                if (AsyncListenerHelper.HandleListenerException(ex, "receiving message", rethrowSocket: true))
                    throw;
            }
            finally
            {
                _cts?.Dispose();
            }
        }

        public void StopListening()
        {
            try
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _udpClient?.Close();
                Console.WriteLine("Stopped listening for UDP messages.");
            }
            catch (Exception ex)
            {
                // ObjectDisposedException (already disposed) or SocketException (already stopped) are expected during cleanup
                AsyncListenerHelper.HandleListenerException(ex, "stopping UDP listener");
            }
        }

        public void Exit()
        {
            StopListening();
        }

        public override int GetHashCode()
        {
            // Use HashCode.Combine for a stable, non-cryptographic hash suitable for GetHashCode
            return HashCode.Combine(_localEndPoint.Address.GetHashCode(), _localEndPoint.Port);
        }

        public override bool Equals(object? obj)
        {
            if (obj is not UdpClientWrapper other)
                return false;

            return _localEndPoint.Address.Equals(other._localEndPoint.Address) &&
                   _localEndPoint.Port.Equals(other._localEndPoint.Port);
        }
    }
}