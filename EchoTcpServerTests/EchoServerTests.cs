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
        Assert.That(started, Is.True, "Server did not start in time");
        Assert.That(server.ActualPort, Is.GreaterThan(0), "Server bound to invalid port");

        // Act: connect client and send data
        using (var client = new TcpClient())
        {
            await client.ConnectAsync("127.0.0.1", server.ActualPort);
            using var stream = client.GetStream();

            var message = Encoding.UTF8.GetBytes("hello-echo");
            await stream.WriteAsync(message.AsMemory());

            // read back
            var buffer = new byte[message.Length];
            var read = await stream.ReadAsync(buffer, 0, buffer.Length);

            Assert.That(read, Is.EqualTo(message.Length), "Echoed byte count differs");
            Assert.That(Encoding.UTF8.GetString(buffer, 0, read), Is.EqualTo("hello-echo"));
        }

        // Cleanup
        server.Stop();
        // give server a moment to shutdown
        await Task.Delay(100);
    }
}
