using NUnit.Framework;
using System;
using System.Threading.Tasks;
using NetSdrClientApp.Networking;
using NetSdrClientApp.Helpers;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class WrapperAndHelpersTests
    {
        [Test]
        public void ToHexString_NullOrEmpty_ReturnsEmpty()
        {
            Assert.That(DebugHelpers.ToHexString((byte[])null), Is.EqualTo(string.Empty));
            Assert.That(DebugHelpers.ToHexString(Array.Empty<byte>()), Is.EqualTo(string.Empty));
        }

        [Test]
        public void ToHexString_ValidBytes_ReturnsHex()
        {
            var data = new byte[] { 0x01, 0x2A, 0xFF };
            Assert.That(DebugHelpers.ToHexString(data), Is.EqualTo("01 2a ff"));
        }

        [Test]
        public void TcpWrapper_NotConnected_BehavesGracefully()
        {
            var tcp = new TcpClientWrapper("localhost", 65000);
            Assert.That(tcp.Connected, Is.False);
            Assert.DoesNotThrow(() => tcp.Disconnect());
            Assert.ThrowsAsync<InvalidOperationException>(async () => await tcp.SendMessageAsync(new byte[] { 1 }));
            Assert.ThrowsAsync<InvalidOperationException>(async () => await tcp.SendMessageAsync("hello"));
        }

        [Test]
        public void UdpWrapper_Equals_GetHashCode_StopExit()
        {
            var a = new UdpClientWrapper(0);
            var b = new UdpClientWrapper(0);

            Assert.That(a.Equals((object?)null), Is.False);
            Assert.That(a.Equals(new object()), Is.False);

            var hash = a.GetHashCode();
            Assert.That(hash, Is.TypeOf<int>());

            Assert.DoesNotThrow(() => a.StopListening());
            Assert.DoesNotThrow(() => a.Exit());
        }
    }
}
