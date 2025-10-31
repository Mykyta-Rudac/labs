using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using Xunit;
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

        // Налаштування для більшості тестів: клієнт підключений
        _tcpMock.Setup(t => t.Connected).Returns(true); 
        
        // Переконайтеся, що SendMessageAsync мокається для повернення Task.CompletedTask
        _tcpMock.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>())).Returns(Task.CompletedTask);

        _client = new NetSdrClient(_tcpMock.Object, _udpMock.Object); 
    }
    
    // --- Тест 1: Перевірка ініціалізації при підключенні ---
    [Fact]
    public async Task ConnectAsync_ShouldCallConnectAndSendThreeInitMessages_WhenNotConnected()
    {
        // Arrange
        // Симулюємо цикл: Непідключений -> Виклик Connect() -> Підключений.
        _tcpMock.SetupSequence(t => t.Connected)
            .Returns(false) // Спочатку не підключений
            .Returns(true)  // Після виклику Connect() в клієнті
            .Returns(true); 
        
        // Act
        await _client.ConnectAsync();

        // Assert
        // 1. Повинен викликатися метод Connect()
        _tcpMock.Verify(t => t.Connect(), Times.Once); 
        
        // 2. Повинно бути відправлено рівно 3 ініціалізаційні повідомлення
        _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
    }

    // --- Тест 2: Підключення не відбувається, якщо вже підключено ---
    [Fact]
    public async Task ConnectAsync_ShouldNotAttemptToConnect_WhenAlreadyConnected()
    {
        // Arrange
        _tcpMock.Setup(t => t.Connected).Returns(true); // Встановлено у конструкторі, але явно для ясності
        
        // Act
        await _client.ConnectAsync();

        // Assert
        // Жодних викликів Connect() або SendMessageAsync, оскільки клієнт вже підключений
        _tcpMock.Verify(t => t.Connect(), Times.Never);
        _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
    }

    // --- Тест 3: Перевірка коректного відключення ---
    [Fact]
    public void Disconnect_ShouldInvokeTcpDisconnect()
    {
        // Act
        _client.Disconect();

        // Assert
        _tcpMock.Verify(t => t.Disconnect(), Times.Once);
    }

    // --- Тест 4: Перевірка запуску IQ (StartIQAsync) ---
    [Fact]
    public async Task StartIQAsync_ShouldSendReceiverStateAndStartUdpListening_WhenConnected()
    {
        // Arrange
        // Перевірка, що IQStarted спочатку false
        _client.IQStarted = false; 

        // Act
        await _client.StartIQAsync();

        // Assert
        // 1. Повинно бути відправлено одне повідомлення ReceiverState
        _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Once); 
        
        // 2. IQStarted має стати true
        Xunit.Assert.True(_client.IQStarted); 
        
        // 3. Має розпочатися прослуховування UDP
        _udpMock.Verify(u => u.StartListeningAsync(), Times.Once);
    }

    // --- Тест 5: Перевірка зупинки IQ (StopIQAsync) ---
    [Fact]
    public async Task StopIQAsync_ShouldSendReceiverStateAndStopUdpListening()
    {
        // Arrange
        _client.IQStarted = true; 

        // Act
        await _client.StopIQAsync();

        // Assert
        // 1. Повинно бути відправлено одне повідомлення ReceiverState (для зупинки)
        _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Once); 
        
        // 2. IQStarted має стати false
        Xunit.Assert.False(_client.IQStarted);
        
        // 3. Має зупинитися прослуховування UDP
        _udpMock.Verify(u => u.StopListening(), Times.Once);
    }

    // --- Тест 6: Перевірка кодування частоти (ChangeFrequencyAsync) ---
    [Fact]
    public async Task ChangeFrequencyAsync_ShouldSendMessage_WithCorrectFrequencyEncoding()
    {
        // Arrange
        long testFrequency = 433920000L; // Висока частота для перевірки кодування
        int channel = 1;

        byte[] sentMessage = null!;
        // Захоплюємо фактично відправлений масив байтів
        _tcpMock.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>()))
                .Callback<byte[]>(msg => sentMessage = msg) 
                .Returns(Task.CompletedTask);

        // Act
        await _client.ChangeFrequencyAsync(testFrequency, channel);

        // Assert
        Xunit.Assert.NotNull(sentMessage);
        
        // Очікувані байти частоти (перші 5 байтів)
        byte[] expectedFrequencyBytes = BitConverter.GetBytes(testFrequency).Take(5).ToArray(); 

        // Очікуваний аргумент: [канал] + [перші 5 байтів частоти]
        byte[] expectedArgs = new byte[] { (byte)channel }.Concat(expectedFrequencyBytes).ToArray();

        // Перевіряємо, що послідовність аргументів (канал + частота) міститься у відправленому повідомленні.
        // Це припускає, що NetSdrMessageHelper.GetControlItemMessage вбудовує ці аргументи в повідомлення.
        bool containsArgs = CheckIfArrayContainsSubarray(sentMessage, expectedArgs);

        Xunit.Assert.True(containsArgs, "Відправлене повідомлення повинно містити байтове представлення каналу та частоти.");
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
}
