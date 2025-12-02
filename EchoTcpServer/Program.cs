using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Helpers;

namespace EchoTcpServer
{
    /// <summary>
    /// This program was designed for test purposes only
    /// Not for a review
    /// </summary>
    public class EchoServer
    {
        private readonly int _port;
        private TcpListener? _listener;
        private CancellationTokenSource? _cancellationTokenSource;

        private readonly TaskCompletionSource<bool>? _startedTcs;

        /// <summary>
        /// Actual port assigned to the listener (useful when caller passes 0 for ephemeral port).
        /// </summary>
        public int ActualPort { get; private set; }

        public EchoServer(int port)
        {
            _port = port;
            _cancellationTokenSource = new CancellationTokenSource();
            _startedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public async Task StartAsync()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            // record the actual bound port (if ephemeral port was requested)
            if (_listener.LocalEndpoint is IPEndPoint ep)
            {
                ActualPort = ep.Port;
            }
            // signal that start completed
            _startedTcs?.TrySetResult(true);
            Console.WriteLine($"Server started on port {ActualPort}.");

            while (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    Console.WriteLine("Client connected.");

                    _ = Task.Run(() => HandleClientAsync(client, _cancellationTokenSource?.Token ?? CancellationToken.None));
                }
                catch (ObjectDisposedException)
                {
                    // Listener has been closed
                    break;
                }
            }

            Console.WriteLine("Server shutdown.");
        }

        /// <summary>
        /// Wait until the server has started and bound to a port (or timeout).
        /// Returns true if server started within timeout, false otherwise.
        /// </summary>
        public async Task<bool> WaitUntilStartedAsync(TimeSpan timeout)
        {
            try
            {
                if (_startedTcs == null)
                    return false;

                await _startedTcs.Task.WaitAsync(timeout);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using (NetworkStream stream = client.GetStream())
            {
                try
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;

                    while (!token.IsCancellationRequested && (bytesRead = await stream.ReadAsync(buffer, token)) > 0)
                    {
                        // Echo back the received message
                        await stream.WriteAsync(buffer.AsMemory(0, bytesRead), token);
                        Console.WriteLine($"Echoed {bytesRead} bytes to the client.");
                    }
                }
                catch (IOException ex)
                {
                    LogHelper.Log($"I/O error: {ex.Message}");
                }
                catch (SocketException ex)
                {
                    LogHelper.LogSocketError("Socket error", ex);
                }
                finally
                {
                    client.Close();
                    Console.WriteLine("Client disconnected.");
                }
            }
        }

        public void Stop()
        {
            if (_cancellationTokenSource != null)
            {
                try { _cancellationTokenSource.Cancel(); } catch (ObjectDisposedException) { /* Already disposed */ }
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            if (_listener != null)
            {
                try { _listener.Stop(); } catch (SocketException) { /* Already stopped */ }
                _listener = null;
            }

            Console.WriteLine("Server stopped.");
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public static async Task Main(string[] args)
        {
            EchoServer server = new EchoServer(5000);

            // Start the server in a separate task
            _ = Task.Run(() => server.StartAsync());

            string host = "127.0.0.1"; // Target IP
            int port = 60000;          // Target Port
            int intervalMilliseconds = 5000; // Send every 3 seconds

            using (var sender = new UdpTimedSender(host, port))
            {
                Console.WriteLine("Press any key to stop sending...");
                sender.StartSending(intervalMilliseconds);

                Console.WriteLine("Press 'q' to quit...");
                while (Console.ReadKey(intercept: true).Key != ConsoleKey.Q)
                {
                    // Just wait until 'q' is pressed
                }

                sender.StopSending();
                server.Stop();
                Console.WriteLine("Sender stopped.");
                await Task.Delay(100);  // Allow server task to process
            }
        }
    }

    public class UdpTimedSender : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly UdpClient _udpClient;
        private Timer? _timer;
        private bool _disposed;

        public UdpTimedSender(string host, int port)
        {
            _host = host;
            _port = port;
            _udpClient = new UdpClient();
        }

        public void StartSending(int intervalMilliseconds)
        {
            if (_timer != null)
                throw new InvalidOperationException("Sender is already running.");

            _timer = new Timer(SendMessageCallback, null, 0, intervalMilliseconds);
        }

        private ushort _sequenceCounter;

        private void SendMessageCallback(object? state)
        {
            try
            {
                //dummy data
                Random rnd = new Random();
                byte[] samples = new byte[1024];
                rnd.NextBytes(samples);
                _sequenceCounter++;

                byte[] msg = (new byte[] { 0x04, 0x84 }).Concat(BitConverter.GetBytes(_sequenceCounter)).Concat(samples).ToArray();
                var endpoint = new IPEndPoint(IPAddress.Parse(_host), _port);

                _udpClient.Send(msg, msg.Length, endpoint);
                Console.WriteLine($"Message sent to {_host}:{_port} ");
            }
            catch (FormatException ex)
            {
                LogHelper.Log($"Invalid host format: {ex.Message}");
            }
            catch (SocketException ex)
            {
                LogHelper.LogSocketError("Socket error sending message", ex);
            }
            catch (ObjectDisposedException ex)
            {
                LogHelper.Log($"UDP client disposed: {ex.Message}");
            }
        }

        public void StopSending()
        {
            _timer?.Dispose();
            _timer = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    StopSending();
                    _udpClient.Dispose();
                }
                _disposed = true;
            }
        }
}
}