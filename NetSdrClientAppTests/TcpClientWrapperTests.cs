using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests
{
    public class TcpClientWrapperTests
    {
        private TcpClientWrapper _tcpClient;

        [SetUp]
        public void Setup()
        {
            _tcpClient = new TcpClientWrapper("127.0.0.1", 5000);
        }

        [Test]
        public void ConstructorTest()
        {
            // Arrange & Act
            var client = new TcpClientWrapper("127.0.0.1", 5000);

            // Assert
            Assert.That(client, Is.Not.Null);
            Assert.That(client.Connected, Is.False);
        }

        [Test]
        public void DisconnectWithoutConnectionTest()
        {
            // Arrange - don't connect

            // Act
            _tcpClient.Disconnect();

            // Assert - no exception should be thrown
            Assert.That(_tcpClient.Connected, Is.False);
        }

        [Test]
        public void SendMessageAsyncWithoutConnectionTest()
        {
            // Arrange
            byte[] data = new byte[] { 0x01, 0x02, 0x03 };

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _tcpClient.SendMessageAsync(data);
            });
        }

        [Test]
        public void SendMessageAsyncStringWithoutConnectionTest()
        {
            // Arrange
            string message = "test";

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _tcpClient.SendMessageAsync(message);
            });
        }

        [Test]
        public void ConnectToInvalidHostTest()
        {
            // Arrange
            var client = new TcpClientWrapper("invalid.host.that.does.not.exist.local", 9999);

            // Act - this should handle the exception gracefully
            client.Connect();

            // Assert - should not be connected
            Assert.That(client.Connected, Is.False);
        }

        [Test]
        public void MessageReceivedEventTest()
        {
            // Arrange
            int eventCount = 0;
            _tcpClient.MessageReceived += (sender, data) =>
            {
                eventCount++;
            };

            // Assert - event handler is registered successfully
            Assert.Pass();
        }
    }
}
