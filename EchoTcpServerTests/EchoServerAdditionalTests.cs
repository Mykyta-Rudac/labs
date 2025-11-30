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

                // call a few times
                mi!.Invoke(sender, new object[] { null });
                mi.Invoke(sender, new object[] { null });
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
                mi!.Invoke(sender, new object[] { null });
            }
            finally
            {
                sender.Dispose();
            }
        }
    }
}
