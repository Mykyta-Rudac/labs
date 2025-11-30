using NUnit.Framework;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using EchoTcpServer;

namespace EchoTcpServerTests;

public class EchoServerTests
{
    [Test]
    public async Task EchoServer_EchosBackData()
    {
        // Arrange: start server on ephemeral port (0)
        var server = new EchoServer(0);
        var serverTask = Task.Run(() => server.StartAsync());

        // Wait until server started and bound
        var started = await server.WaitUntilStartedAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(started, "Server did not start in time");
        Assert.Greater(server.ActualPort, 0, "Server bound to invalid port");

        // Act: connect client and send data
        using (var client = new TcpClient())
        {
            await client.ConnectAsync("127.0.0.1", server.ActualPort);
            using var stream = client.GetStream();

            var message = Encoding.UTF8.GetBytes("hello-echo");
            await stream.WriteAsync(message, 0, message.Length);

            // read back
            var buffer = new byte[message.Length];
            var read = await stream.ReadAsync(buffer, 0, buffer.Length);

            Assert.AreEqual(message.Length, read, "Echoed byte count differs");
            Assert.AreEqual("hello-echo", Encoding.UTF8.GetString(buffer, 0, read));
        }

        // Cleanup
        server.Stop();
        // give server a moment to shutdown
        await Task.Delay(100);
    }
}
