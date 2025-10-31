using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using Xunit;
using System.Threading.Tasks;
using System;
using System.Linq; // Додано для використання методу SequenceEqual

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
        // Припускаємо, що NetSdrClient має конструктор, який приймає ITcpClient та IUdpClient
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
        // Перевірка, що було відправлено рівно 3 ініціалізаційні повідомлення
        _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
    }

    [Fact]
    public void Disconnect_ShouldInvokeTcpDisconnect()
    {
        // Act
        _client.Disconect(); // Примітка: Скоріш за все, метод має називатися Disconnect()

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
        Assert.True(_client.IQStarted);
        _udpMock.Verify(u => u.StartListeningAsync(), Times.Once);
    }

    [Fact]
    public async Task StopIQAsync_ShouldSetIQStartedFalse_AndStopUdpListening()
    {
        // Arrange
        _tcpMock.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>())).Returns(Task.CompletedTask);
        // Встановлюємо IQStarted = true для коректної перевірки зміни
        _client.IQStarted = true; 

        // Act
        await _client.StopIQAsync();

        // Assert
        Assert.False(_client.IQStarted);
        _udpMock.Verify(u => u.StopListening(), Times.Once);
    }

    [Fact]
    public async Task ChangeFrequencyAsync_ShouldSendMessage_WithCorrectArgs()
    {
        // Arrange
        long testFrequency = 123456789L; // Використовуємо більшу частоту для надійності
        int channel = 2;

        byte[] sentMessage = null!;
        _tcpMock.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>()))
                // Використовуємо Callback для захоплення фактично відправленого масиву байтів
                .Callback<byte[]>(msg => sentMessage = msg) 
                .Returns(Task.CompletedTask);

        // Act
        await _client.ChangeFrequencyAsync(testFrequency, channel);

        // Assert
        _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
        Assert.NotNull(sentMessage);

        // 1. Перевірка, що повідомлення містить байт каналу
        Assert.Contains(sentMessage, b => b == (byte)channel);

        // 2. Додана перевірка: Отримуємо байтове представлення частоти
        // Примітка: Це припускає, що NetSdrClient використовує BitConverter для кодування long
        byte[] frequencyBytes = BitConverter.GetBytes(testFrequency); 

        // 3. Перевірка, що послідовність байтів частоти міститься у відправленому повідомленні
        // Це вимагає, щоб порядок байтів був послідовним і без розривів.
        bool containsFrequency = CheckIfArrayContainsSubarray(sentMessage, frequencyBytes);

        Assert.True(containsFrequency, "Відправлене повідомлення повинно містити байтове представлення частоти.");
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
        // Перевірка, що слухання UDP ніколи не запускалося
        _udpMock.Verify(u => u.StartListeningAsync(), Times.Never); 
        Assert.False(_client.IQStarted);
    }
}