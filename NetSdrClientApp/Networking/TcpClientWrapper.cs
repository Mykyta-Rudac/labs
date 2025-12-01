using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetSdrClientApp.Networking
{
    public class TcpClientWrapper : ITcpClient
    {
        private readonly string _host;
        private readonly int _port;
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;

        public bool Connected => _tcpClient != null && _tcpClient.Connected && _stream != null;

        public event EventHandler<byte[]>? MessageReceived;

        public TcpClientWrapper(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public void Connect()
        {
            if (Connected)
            {
                Console.WriteLine($"Already connected to {_host}:{_port}");
                return;
            }

            _tcpClient = new TcpClient();

            try
            {
                _cts = new CancellationTokenSource();
                _tcpClient.Connect(_host, _port);
                _stream = _tcpClient.GetStream();
                Console.WriteLine($"Connected to {_host}:{_port}");
                _ = StartListeningAsync();
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket error while connecting: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Invalid operation while connecting: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            if (Connected)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _stream?.Close();
                _tcpClient?.Close();

                _cts = null;
                _tcpClient = null;
                _stream = null;
                Console.WriteLine("Disconnected.");
            }
            else
            {
                Console.WriteLine("No active connection to disconnect.");
            }
        }

        public async Task SendMessageAsync(byte[] data)
        {
            if (Connected && _stream != null && _stream.CanWrite)
            {
                Console.WriteLine($"Message sent: " + NetSdrClientApp.Helpers.DebugHelpers.ToHexString(data));
                await _stream.WriteAsync(data.AsMemory());
            }
            else
            {
                throw new InvalidOperationException("Not connected to a server.");
            }
        }

        public async Task SendMessageAsync(string str)
        {
            var data = Encoding.UTF8.GetBytes(str);
            if (Connected && _stream != null && _stream.CanWrite)
            {
                Console.WriteLine($"Message sent: " + NetSdrClientApp.Helpers.DebugHelpers.ToHexString(data));
                await _stream.WriteAsync(data.AsMemory());
            }
            else
            {
                throw new InvalidOperationException("Not connected to a server.");
            }
        }

        private async Task StartListeningAsync()
        {
            if (Connected && _stream != null && _stream.CanRead)
            {
                try
                {
                    Console.WriteLine($"Starting listening for incomming messages.");

                    while (_cts != null && !_cts.Token.IsCancellationRequested)
                    {
                        byte[] buffer = new byte[8194];

                        int bytesRead = await _stream.ReadAsync(buffer, _cts.Token);
                        if (bytesRead > 0)
                        {
                            MessageReceived?.Invoke(this, buffer.AsSpan(0, bytesRead).ToArray());
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Operation was cancelled
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"I/O error in listening loop: {ex.Message}");
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Socket error in listening loop: {ex.Message}");
                }
                finally
                {
                    Console.WriteLine("Listener stopped.");
                    _cts?.Dispose();
                    _cts = null;
                }
            }
            else
            {
                throw new InvalidOperationException("Not connected to a server.");
            }
        }
    }

}
