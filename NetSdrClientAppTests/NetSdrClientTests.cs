using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests;

public class NetSdrClientTests
{
    NetSdrClient _client;
    Mock<ITcpClient> _tcpMock;
    Mock<IUdpClient> _updMock;

    public NetSdrClientTests() { }

    [SetUp]
    public void Setup()
    {
        _tcpMock = new Mock<ITcpClient>();
        _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
        });

        _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
        });

        _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>())).Callback<byte[]>((bytes) =>
        {
            _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, bytes);
        });

        _updMock = new Mock<IUdpClient>();

        _client = new NetSdrClient(_tcpMock.Object, _updMock.Object);
    }

    [Test]
    public async Task ConnectAsyncTest()
    {
        //act
        await _client.ConnectAsync();

        //assert
        _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
    }

    [Test]
    public async Task DisconnectWithNoConnectionTest()
    {
        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task DisconnectTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task StartIQNoConnectionTest()
    {

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        _tcpMock.VerifyGet(tcp => tcp.Connected, Times.AtLeastOnce);
    }

    [Test]
    public async Task StartIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
        Assert.That(_client.IQStarted, Is.True);
    }

    [Test]
    public async Task StopIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StopIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(tcp => tcp.StopListening(), Times.Once);
        Assert.That(_client.IQStarted, Is.False);
    }

    //TODO: cover the rest of the NetSdrClient code here
using System;
using System.Threading.Tasks;
using Moq;
using Xunit;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using NetSdrClientApp.Messages;

namespace NetSdrClientAppTests
{
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
            _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.AtLeastOnce);
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
            long testFrequency = 123456;
            int channel = 2;

            byte[] sentMessage = null!;
            _tcpMock.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>()))
                    .Callback<byte[]>(msg => sentMessage = msg)
                    .Returns(Task.CompletedTask);

            // Act
            await _client.ChangeFrequencyAsync(testFrequency, channel);

            // Assert
            _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
            Assert.NotNull(sentMessage);
            Assert.Contains(sentMessage, b => b == (byte)channel);
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
        public async Task StartIQAsync_ShouldNotRun_WhenTcpNotConnected()
        {
            // Arrange
            _tcpMock.Setup(t => t.Connected).Returns(false);

            // Act
            await _client.StartIQAsync();

            // Assert
            _udpMock.Verify(u => u.StartListeningAsync(), Times.Never);
            Assert.False(_client.IQStarted);
        }
    }
}
    
}
