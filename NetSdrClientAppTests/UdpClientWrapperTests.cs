using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests
{
    public class UdpClientWrapperTests
    {
        private UdpClientWrapper _udpClient;

        [SetUp]
        public void Setup()
        {
            _udpClient = new UdpClientWrapper(60000);
        }

        [Test]
        public void ConstructorTest()
        {
            // Arrange & Act
            var client = new UdpClientWrapper(50000);

            // Assert
            Assert.That(client, Is.Not.Null);
        }

        [Test]
        public async Task StartListeningAsyncTest()
        {
            // Arrange
            bool messageReceived = false;
            _udpClient.MessageReceived += (sender, data) =>
            {
                messageReceived = true;
            };

            // Act
            var listeningTask = _udpClient.StartListeningAsync();

            // Give it a moment to start listening
            await Task.Delay(100);

            // Assert - we're just checking it starts without throwing
            Assert.That(listeningTask, Is.Not.Null);

            // Cleanup
            _udpClient.StopListening();
        }

        [Test]
        public void StopListeningTest()
        {
            // Arrange & Act
            var listeningTask = _udpClient.StartListeningAsync();
            Task.Delay(100).Wait();

            // Act
            _udpClient.StopListening();

            // Assert - no exception should be thrown
            Assert.Pass();
        }

        [Test]
        public void ExitTest()
        {
            // Arrange & Act
            var listeningTask = _udpClient.StartListeningAsync();
            Task.Delay(100).Wait();

            // Act
            _udpClient.Exit();

            // Assert - no exception should be thrown
            Assert.Pass();
        }

        [Test]
        public void GetHashCodeTest()
        {
            // Arrange
            var client1 = new UdpClientWrapper(60000);
            var client2 = new UdpClientWrapper(60000);
            var client3 = new UdpClientWrapper(50000);

            // Act
            int hash1 = client1.GetHashCode();
            int hash2 = client2.GetHashCode();
            int hash3 = client3.GetHashCode();

            // Assert
            Assert.That(hash1, Is.EqualTo(hash2));
            Assert.That(hash1, Is.Not.EqualTo(hash3));
        }

        [Test]
        public void EqualsTest()
        {
            // Arrange
            var client1 = new UdpClientWrapper(60000);
            var client2 = new UdpClientWrapper(60000);
            var client3 = new UdpClientWrapper(50000);

            // Act & Assert
            Assert.That(client1.Equals(client2), Is.True);
            Assert.That(client1.Equals(client3), Is.False);
            Assert.That(client1.Equals(null), Is.False);
            Assert.That(client1.Equals("string"), Is.False);
        }

        [Test]
        public void StopListeningWhenNotListeningTest()
        {
            // Arrange - don't start listening

            // Act
            _udpClient.StopListening();

            // Assert - no exception should be thrown
            Assert.Pass();
        }

        [Test]
        public void ExitWhenNotListeningTest()
        {
            // Arrange - don't start listening

            // Act
            _udpClient.Exit();

            // Assert - no exception should be thrown
            Assert.Pass();
        }
    }
}
