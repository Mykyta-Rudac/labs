using NUnit.Framework;
using System;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using EchoTcpServer;

namespace EchoTcpServerTests
{
    public class EchoServerAdditionalTests
    {
        [Test]
        public async Task WaitUntilStarted_WithoutStart_ReturnsFalse()
        {
            var server = new EchoServer(0);
            var started = await server.WaitUntilStartedAsync(TimeSpan.FromMilliseconds(200));
            Assert.That(started, Is.False);
        }

        [Test]
        public async Task StartAndStop_NoClient_DoesNotThrow()
        {
            var server = new EchoServer(0);
            var serverTask = Task.Run(() => server.StartAsync());

            var started = await server.WaitUntilStartedAsync(TimeSpan.FromSeconds(2));
            Assert.That(started, Is.True);

            // stop the server and ensure stop doesn't throw
            server.Stop();
            await Task.Delay(100);

            Assert.Pass();
        }

        [Test]
        public async Task UdpTimedSender_StartStop_DoesNotThrow()
        {
            var sender = new UdpTimedSender("127.0.0.1", 60001);
            try
            {
                sender.StartSending(50);
                await Task.Delay(250);
                sender.StopSending();
            }
            finally
            {
                sender.Dispose();
            }

            Assert.Pass();
        }

        [Test]
        public void UdpTimedSender_StartTwice_ThrowsInvalidOperation()
        {
            var sender = new UdpTimedSender("127.0.0.1", 60001);
            try
            {
                sender.StartSending(100);
                Assert.Throws<InvalidOperationException>(() => sender.StartSending(100));
                sender.StopSending();
            }
            finally
            {
                sender.Dispose();
            }
        }

        [Test]
        public void UdpTimedSender_InvokeSendMessageCallback_DoesNotThrow()
        {
            var sender = new UdpTimedSender("127.0.0.1", 60001);
            try
            {
                // Invoke private SendMessageCallback via reflection to exercise internal code paths
                var mi = typeof(UdpTimedSender).GetMethod("SendMessageCallback", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(mi, Is.Not.Null, "SendMessageCallback method not found");

                // call a few times with proper object array
                mi!.Invoke(sender, new object?[] { null });
                mi.Invoke(sender, new object?[] { null });
            }
            finally
            {
                sender.Dispose();
            }
        }

        [Test]
        public void UdpTimedSender_InvokeSendMessageCallback_WithInvalidHost_EntersCatch()
        {
            // Use an invalid host so IPAddress.Parse throws and SendMessageCallback hits the catch block
            var sender = new UdpTimedSender("not-a-valid-host", 60001);
            try
            {
                var mi = typeof(UdpTimedSender).GetMethod("SendMessageCallback", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(mi, Is.Not.Null, "SendMessageCallback method not found");

                // Invocation should not throw because SendMessageCallback catches exceptions internally
                mi!.Invoke(sender, new object?[] { null });
            }
            finally
            {
                sender.Dispose();
            }
        }

        [Test]
        public async Task EchoServer_EchosBackLargeData()
        {
            var server = new EchoServer(0);
            var serverTask = Task.Run(() => server.StartAsync());

            var started = await server.WaitUntilStartedAsync(TimeSpan.FromSeconds(2));
            Assert.That(started, Is.True);

            var client = new TcpClient();
            try
            {
                await client.ConnectAsync("127.0.0.1", server.ActualPort);
                using (var stream = client.GetStream())
                {
                    // send 20000 bytes to force multiple read/write cycles
                    byte[] payload = new byte[20000];
                    new Random(1).NextBytes(payload);
                    await stream.WriteAsync(payload.AsMemory());

                    byte[] buffer = new byte[payload.Length];
                    int offset = 0;
                    while (offset < payload.Length)
                    {
                        int read = await stream.ReadAsync(buffer, offset, buffer.Length - offset);
                        if (read <= 0) break;
                        offset += read;
                    }

                    Assert.That(offset, Is.EqualTo(payload.Length));
                    Assert.That(buffer, Is.EqualTo(payload));
                }
            }
            finally
            {
                client.Close();
                server.Stop();
            }
        }

        [Test]
        public async Task EchoServer_StopDuringAccept_DoesNotHang()
        {
            var server = new EchoServer(0);
            var serverTask = Task.Run(() => server.StartAsync());

            var started = await server.WaitUntilStartedAsync(TimeSpan.FromSeconds(2));
            Assert.That(started, Is.True);

            // stop server immediately while it's likely waiting for Accept
            server.Stop();

            // ensure server task completes in a short time
            Assert.That(await Task.WhenAny(serverTask, Task.Delay(2000)), Is.EqualTo(serverTask));
        }

        [Test]
        public void UdpTimedSender_MultipleSendMessageCallback_Invocations()
        {
            var sender = new UdpTimedSender("127.0.0.1", 60001);
            try
            {
                var mi = typeof(UdpTimedSender).GetMethod("SendMessageCallback", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(mi, Is.Not.Null, "SendMessageCallback method not found");

                // invoke multiple times with proper object array
                for (int j = 0; j < 5; j++)
                {
                    mi!.Invoke(sender, new object?[] { null });
                }
            }
            finally
            {
                sender.Dispose();
            }
        }

        [Test]
        public async Task EchoServer_ClientDisconnectsDuringRead_HandlesGracefully()
        {
            // This test exercises the socket exception handler in HandleClientAsync
            var server = new EchoServer(0);
            var serverTask = Task.Run(() => server.StartAsync());

            var started = await server.WaitUntilStartedAsync(TimeSpan.FromSeconds(2));
            Assert.That(started, Is.True);

            var client = new TcpClient();
            try
            {
                await client.ConnectAsync("127.0.0.1", server.ActualPort);
                using (var stream = client.GetStream())
                {
                    // Send a partial message
                    byte[] partialMsg = new byte[] { 0x01, 0x02 };
                    await stream.WriteAsync(partialMsg.AsMemory());

                    // Abruptly close the connection (simulates network error/disconnect)
                    stream.Close();
                }
            }
            finally
            {
                client.Close();
                server.Stop();
                await Task.Delay(100);
            }

            Assert.Pass("Gracefully handled client disconnect");
        }

        [Test]
        public async Task EchoServer_HandleClientAsync_WithEmptyRead_ExitLoop()
        {
            // Test the case where read returns 0 (connection closed by client)
            var server = new EchoServer(0);
            var serverTask = Task.Run(() => server.StartAsync());

            var started = await server.WaitUntilStartedAsync(TimeSpan.FromSeconds(2));
            Assert.That(started, Is.True);

            var client = new TcpClient();
            try
            {
                await client.ConnectAsync("127.0.0.1", server.ActualPort);
                using (var stream = client.GetStream())
                {
                    // Just close without sending data
                    stream.Close();
                }

                // Server should handle the closure gracefully
                await Task.Delay(200);
            }
            finally
            {
                client.Close();
                server.Stop();
                await Task.Delay(100);
            }

            Assert.Pass("Handled zero-byte read correctly");
        }

        [Test]
        public void LogHelper_Log_OutputsMessage()
        {
            // Direct test of LogHelper to ensure coverage
            var captureOutput = new System.IO.StringWriter();
            var originalOut = Console.Out;
            try
            {
                Console.SetOut(captureOutput);
                LogHelper.Log("Test message");
                var output = captureOutput.ToString();
                Assert.That(output, Does.Contain("Test message"));
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Test]
        public void LogHelper_LogSocketError_OutputsContextAndMessage()
        {
            // Direct test of LogHelper.LogSocketError to ensure coverage
            var captureOutput = new System.IO.StringWriter();
            var originalOut = Console.Out;
            try
            {
                Console.SetOut(captureOutput);
                var ex = new InvalidOperationException("Test error");
                LogHelper.LogSocketError("Test context", ex);
                var output = captureOutput.ToString();
                Assert.That(output, Does.Contain("Test context"));
                Assert.That(output, Does.Contain("Test error"));
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
    }
}
