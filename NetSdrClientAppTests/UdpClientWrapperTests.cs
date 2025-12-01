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
            _udpClient.MessageReceived += (sender, data) =>
            {
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
        public void StopListeningWhenNotListeningTest()
        {
            // Arrange - don't start listening

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
