using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using Xunit; // Використовуємо Xunit
using System.Threading.Tasks;
using System;
using System.Linq; 

namespace NetSdrClientAppTests;

public class NetSdrClientTests
{
    private readonly Mock<ITcpClient> _tcpMock;
    private readonly Mock<IUdpClient> _udpMock;
    private readonly NetSdrClient _client;

    public NetSdrClientTests()
    {
        _tcpMock = new Mock<ITcpClient>();
        _udpMock = new Mock<IUdpClient>();

        _tcpMock.Setup(t => t.Connected).Returns(true);
        _client = new NetSdrClient(_tcpMock.Object, _udpMock.Object); 
    }

    [Fact]
    public async Task ConnectAsync_ShouldSendInitializationMessages_WhenNotConnected()
    {
        // Arrange
        _tcpMock.Setup(t => t.Connected).Returns(false);

        // Act
        await _client.ConnectAsync();

        // Assert
        _tcpMock.Verify(t => t.Connect(), Times.Once);
        _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
    }

    [Fact]
    public void Disconnect_ShouldInvokeTcpDisconnect()
    {
        // Act
        _client.Disconect();

        // Assert
        _tcpMock.Verify(t => t.Disconnect(), Times.Once);
    }

    [Fact]
    public async Task StartIQAsync_ShouldSetIQStartedTrue_AndStartUdpListening()
    {
        // Arrange
        _tcpMock.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>())).Returns(Task.CompletedTask);

        // Act
        await _client.StartIQAsync();

        // Assert
        // Явно вказуємо Xunit.Assert
        Xunit.Assert.True(_client.IQStarted); 
        _udpMock.Verify(u => u.StartListeningAsync(), Times.Once);
    }

    [Fact]
    public async Task StopIQAsync_ShouldSetIQStartedFalse_AndStopUdpListening()
    {
        // Arrange
        _tcpMock.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>())).Returns(Task.CompletedTask);
        _client.IQStarted = true; 

        // Act
        await _client.StopIQAsync();

        // Assert
        // Явно вказуємо Xunit.Assert
        Xunit.Assert.False(_client.IQStarted); 
        _udpMock.Verify(u => u.StopListening(), Times.Once);
    }

    [Fact]
    public async Task ChangeFrequencyAsync_ShouldSendMessage_WithCorrectArgs()
    {
        // Arrange
        long testFrequency = 123456789L;
        int channel = 2;

        byte[] sentMessage = null!;
        _tcpMock.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>()))
                .Callback<byte[]>(msg => sentMessage = msg) 
                .Returns(Task.CompletedTask);

        // Act
        await _client.ChangeFrequencyAsync(testFrequency, channel);

        // Assert
        _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
        // Явно вказуємо Xunit.Assert
        Xunit.Assert.NotNull(sentMessage);
        // Явно вказуємо Xunit.Assert
        Xunit.Assert.Contains(sentMessage, b => b == (byte)channel);

        byte[] frequencyBytes = BitConverter.GetBytes(testFrequency); 

        bool containsFrequency = CheckIfArrayContainsSubarray(sentMessage, frequencyBytes);

        // Явно вказуємо Xunit.Assert
        Xunit.Assert.True(containsFrequency, "Відправлене повідомлення повинно містити байтове представлення частоти.");
    }
    
    // Допоміжний метод для перевірки, чи міститься підмасив (subArray) в основному масиві (mainArray)
    private bool CheckIfArrayContainsSubarray(byte[] mainArray, byte[] subArray)
    {
        for (int i = 0; i <= mainArray.Length - subArray.Length; i++)
        {
            if (mainArray.Skip(i).Take(subArray.Length).SequenceEqual(subArray))
            {
                return true;
            }
        }
        return false;
    }

    [Fact]
    public async Task StartIQAsync_ShouldNotRun_WhenTcpNotConnected()
    {
        // Arrange
        _tcpMock.Setup(t => t.Connected).Returns(false);

        // Act
        await _client.StartIQAsync();

        // Assert
        _udpMock.Verify(u => u.StartListeningAsync(), Times.Never); 
        // Явно вказуємо Xunit.Assert
        Xunit.Assert.False(_client.IQStarted); 
    }
}