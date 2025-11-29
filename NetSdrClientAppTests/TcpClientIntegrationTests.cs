using NetSdrClientApp.Networking;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetSdrClientAppTests
{
    public class TcpClientIntegrationTests
    {
        [Test]
        public async Task Connect_Send_Receive_EndToEnd()
        {
            // Start a TcpListener on loopback with ephemeral port
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            var clientWrapper = new TcpClientWrapper("127.0.0.1", port);

            // Accept the server-side connection asynchronously
            var acceptTask = Task.Run(async () =>
            {
                var serverClient = await listener.AcceptTcpClientAsync();
                return serverClient;
            });

            // Connect the wrapper (client side)
            clientWrapper.Connect();

            var serverSide = await acceptTask;
            Assert.That(clientWrapper.Connected, Is.True);

            // Prepare to read data sent by wrapper
            var serverStream = serverSide.GetStream();
            var readBuffer = new byte[1024];
            var serverReadTask = serverStream.ReadAsync(readBuffer, 0, readBuffer.Length);

            // Send message from wrapper to server
            var payload = Encoding.UTF8.GetBytes("hello-server");
            await clientWrapper.SendMessageAsync(payload);

            // Wait for server to receive
            var bytesRead = await serverReadTask;
            Assert.That(bytesRead, Is.GreaterThan(0));
            var received = Encoding.UTF8.GetString(readBuffer, 0, bytesRead);
            Assert.That(received, Does.Contain("hello-server"));

            // Now test server->client message reception
            var tcs = new TaskCompletionSource<byte[]>();
            clientWrapper.MessageReceived += (s, data) => tcs.TrySetResult(data);

            var serverWrite = Encoding.UTF8.GetBytes("hello-client");
            await serverStream.WriteAsync(serverWrite, 0, serverWrite.Length);

            var clientReceived = await Task.WhenAny(tcs.Task, Task.Delay(1000));
            Assert.That(clientReceived == tcs.Task, Is.True, "Client did not receive message in time");
            Assert.That(Encoding.UTF8.GetString(tcs.Task.Result), Does.Contain("hello-client"));

            // Cleanup
            clientWrapper.Disconnect();
            serverSide.Close();
            listener.Stop();
        }
    }
}
