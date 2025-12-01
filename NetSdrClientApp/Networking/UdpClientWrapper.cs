using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
            catch (OperationCanceledException)
            {
                // Operation was cancelled
            }
            catch (SocketException ex)
            {
                NetSdrClientApp.Helpers.LogHelper.LogSocketError("Socket error receiving message", ex);
                throw;
            }
            catch (ObjectDisposedException)
            {
                // socket disposed while stopping
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
            catch (ObjectDisposedException)
            {
                // already disposed
            }
            catch (SocketException ex)
            {
                NetSdrClientApp.Helpers.LogHelper.LogSocketError("Socket error while stopping", ex);
            }
        }

        public void Exit()
        {
            StopListening();
        }

        public override int GetHashCode()
        {
            var payload = $"{nameof(UdpClientWrapper)}|{_localEndPoint.Address}|{_localEndPoint.Port}";

            var hash = MD5.HashData(Encoding.UTF8.GetBytes(payload));

            return BitConverter.ToInt32(hash, 0);
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