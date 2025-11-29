using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests;

/// <summary>
/// Additional tests for NetSdrClient advanced functionality (frequency change, message handlers, etc.).
/// Inherits common mock setup from NetSdrClientTestBase to eliminate code duplication.
/// </summary>
public class NetSdrClientAdditionalTests : NetSdrClientTestBase
{

    [Test]
    public async Task ChangeFrequencyAsyncTest()
    {
        // Arrange
        await _client.ConnectAsync();

        // Act
        await _client.ChangeFrequencyAsync(20000000, 1);

        // Assert
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(4)); // 3 from Connect + 1 from ChangeFrequency
    }

    [Test]
    public async Task ChangeFrequencyAsyncWithoutConnectionTest()
    {
        // Act
        await _client.ChangeFrequencyAsync(20000000, 1);

        // Assert
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
    }

    [Test]
    public async Task StopIQWithoutConnectionTest()
    {
        // Act
        await _client.StopIQAsync();

        // Assert
        _udpMock.Verify(udp => udp.StopListening(), Times.Never);
        Assert.That(_client.IQStarted, Is.False);
    }

    [Test]
    public async Task TcpMessageReceivedHandlerTest()
    {
        // Arrange
        await _client.ConnectAsync();
        var responseBytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        // Act - Raise the TCP MessageReceived event
        _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, responseBytes);

        // Assert - No exception should be thrown
        Assert.Pass("TCP message handler executed without exception");
    }

    [Test]
    public async Task UdpMessageReceivedHandlerTest()
    {
        // Arrange
        await _client.ConnectAsync();
        var udpMessageBytes = new byte[] { 0x09, 0x00, 0xB8, 0x00, 0xA0, 0x86, 0x10, 0x00 };

        // Act - Raise the UDP MessageReceived event
        _udpMock.Raise(udp => udp.MessageReceived += null, _udpMock.Object, udpMessageBytes);

        // Assert - No exception should be thrown
        Assert.Pass("UDP message handler executed without exception");
    }

    [Test]
    public async Task UdpMessageReceivedHandlerWithValidDataTest()
    {
        // Arrange
        await _client.ConnectAsync();
        // Create a valid NetSdr message with sample data
        var udpMessageBytes = new byte[] 
        { 
            0x09, 0x00, // sequence number
            0x10, 0x00, // 16 samples
            0x01, 0x00, // sample 1
            0x02, 0x00, // sample 2
            0x03, 0x00, // sample 3
            0x04, 0x00  // sample 4
        };

        // Act - Raise the UDP MessageReceived event
        _udpMock.Raise(udp => udp.MessageReceived += null, _udpMock.Object, udpMessageBytes);

        // Assert - No exception should be thrown
        Assert.Pass("UDP message handler with valid data executed without exception");
    }

    [Test]
    public async Task MultipleConnectAttemptsTest()
    {
        // Act - Connect multiple times
        await _client.ConnectAsync();
        await _client.ConnectAsync(); // Should not send messages again if already connected

        // Assert - SendMessageAsync should be called exactly 3 times (from first Connect only)
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
    }
}
