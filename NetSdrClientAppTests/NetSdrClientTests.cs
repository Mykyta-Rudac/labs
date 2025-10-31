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
    
    // ВАЖЛИВО: Оскільки ми будемо змінювати мок t.Connected у Тесті 1,
    // видаляємо глобальне налаштування Connected з конструктора, 
    // щоб не конфліктувати з налаштуванням початкового стану у Тесті 1.
    // Замість цього, ми налаштуємо поведінку Connected в кожному тесті явно.
    public NetSdrClientTests()
    {
        _tcpMock = new Mock<ITcpClient>();
        _udpMock = new Mock<IUdpClient>();
        
        // Переконайтеся, що SendMessageAsync мокається для повернення Task.CompletedTask
        _tcpMock.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>())).Returns(Task.CompletedTask);

        _client = new NetSdrClient(_tcpMock.Object, _udpMock.Object); 
    }
    
    // Допоміжний метод для симуляції відповіді TCP
    private void SimulateTcpResponse(byte[] response)
    {
        _tcpMock.Raise(t => t.MessageReceived += null, _tcpMock.Object, response);
    }
    
    // --- Тест 1: Перевірка ініціалізації при підключенні ---
    [Fact]
    public async Task ConnectAsync_ShouldCallConnectAndSendThreeInitMessages_WhenNotConnected()
    {
        // Arrange
        _tcpMock.Invocations.Clear(); 
        
        // 1. Встановлюємо початковий стан: НЕ ПІДКЛЮЧЕНИЙ
        _tcpMock.Setup(t => t.Connected).Returns(false);
        
        // 2. Встановлюємо, що після виклику t.Connect() стан ПІДКЛЮЧЕНИЙ стає true
        _tcpMock.Setup(t => t.Connect()).Callback(() => 
        {
            _tcpMock.Setup(t => t.Connected).Returns(true);
        });

        // 3. Налаштовуємо мок для симуляції трьох відповідей TCP 
        // (це також вирішує проблему зависання, як ми робили раніше)
        _tcpMock.SetupSequence(t => t.SendMessageAsync(It.IsAny<byte[]>()))
                .Returns(() => { SimulateTcpResponse(new byte[] { 0x01 }); return Task.CompletedTask; })
                .Returns(() => { SimulateTcpResponse(new byte[] { 0x02 }); return Task.CompletedTask; })
                .Returns(() => { SimulateTcpResponse(new byte[] { 0x03 }); return Task.CompletedTask; });
        
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
        // Налаштовуємо початковий стан: ПІДКЛЮЧЕНИЙ
        _tcpMock.Setup(t => t.Connected).Returns(true);
        
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
        _tcpMock.Setup(t => t.Connected).Returns(true);
        _client.IQStarted = false; 

        // !!! ВИПРАВЛЕННЯ ДЛЯ ЗАВИСАННЯ: Симулюємо відповідь TCP одразу після відправки повідомлення !!!
        _tcpMock.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>()))
                .Returns(() => { 
                    // Симулюємо відповідь сервера (наприклад, байт 0xFF), щоб завершити TaskCompletionSource
                    SimulateTcpResponse(new byte[] { 0xFF }); 
                    return Task.CompletedTask; 
                });

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
        _tcpMock.Setup(t => t.Connected).Returns(true);
        _client.IQStarted = true; 
        
        // !!! ВИПРАВЛЕННЯ ДЛЯ ЗАВИСАННЯ: Симулюємо відповідь TCP одразу після відправки повідомлення !!!
        _tcpMock.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>()))
                .Returns(() => { 
                    SimulateTcpResponse(new byte[] { 0xFE }); 
                    return Task.CompletedTask; 
                });

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
        _tcpMock.Setup(t => t.Connected).Returns(true);
        long testFrequency = 433920000L;
        int channel = 1;

        byte[] sentMessage = null!;
        // !!! ВИПРАВЛЕННЯ ДЛЯ ЗАВИСАННЯ: Симулюємо відповідь TCP одразу після відправки повідомлення !!!
        _tcpMock.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>()))
                .Callback<byte[]>(msg => sentMessage = msg) 
                .Returns(() => { 
                    SimulateTcpResponse(new byte[] { 0xFD }); 
                    return Task.CompletedTask; 
                });

        // Act
        await _client.ChangeFrequencyAsync(testFrequency, channel);

        // Assert
        Xunit.Assert.NotNull(sentMessage);
        
        // Очікувані байти частоти (перші 5 байтів)
        byte[] expectedFrequencyBytes = BitConverter.GetBytes(testFrequency).Take(5).ToArray(); 

        // Очікуваний аргумент: [канал] + [перші 5 байтів частоти]
        byte[] expectedArgs = new byte[] { (byte)channel }.Concat(expectedFrequencyBytes).ToArray();

        // Перевіряємо, що послідовність аргументів (канал + частота) міститься у відправленому повідомленні.
        bool containsArgs = CheckIfArrayContainsSubarray(sentMessage, expectedArgs);

        Xunit.Assert.True(containsArgs, "Відправлене повідомлення повинно містити байтове представлення каналу та частоти.");
    }

    // --- Тест 7: Перевірка, що StartIQAsync не виконується без підключення ---
    [Fact]
    public async Task StartIQAsync_ShouldNotRun_WhenTcpNotConnected()
    {
        // Arrange
        _tcpMock.Setup(t => t.Connected).Returns(false);
        _client.IQStarted = false; 

        // Act
        await _client.StartIQAsync();

        // Assert
        // 1. Не повинно бути жодних викликів SendMessageAsync
        _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Never); 
        
        // 2. Прослуховування UDP не повинно запускатися
        _udpMock.Verify(u => u.StartListeningAsync(), Times.Never); 

        // 3. Стан IQStarted має залишитися false
        Xunit.Assert.False(_client.IQStarted); 
    }

    // --- Тест 8: Перевірка, що StopIQAsync не виконується без підключення ---
    [Fact]
    public async Task StopIQAsync_ShouldNotRun_WhenTcpNotConnected()
    {
        // Arrange
        _tcpMock.Setup(t => t.Connected).Returns(false);
        _client.IQStarted = true; // Стан був true до спроби зупинки

        // Act
        await _client.StopIQAsync();

        // Assert
        // 1. Не повинно бути жодних викликів SendMessageAsync
        _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Never); 
        
        // 2. Прослуховування UDP не повинно зупинятися (StopListening)
        _udpMock.Verify(u => u.StopListening(), Times.Never); 

        // 3. Стан IQStarted має залишитися true (логіка не відпрацювала)
        Xunit.Assert.True(_client.IQStarted); 
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
