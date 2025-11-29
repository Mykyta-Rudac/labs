using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests;

/// <summary>
/// Base class for NetSdrClient tests with common setup and mock configuration.
/// Eliminates code duplication between NetSdrClientTests and NetSdrClientAdditionalTests.
/// </summary>
public abstract class NetSdrClientTestBase
{
    protected NetSdrClient _client;
    protected Mock<ITcpClient> _tcpMock;
    protected Mock<IUdpClient> _udpMock;

    [SetUp]
    public void BaseSetup()
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

        _udpMock = new Mock<IUdpClient>();

        _client = new NetSdrClient(_tcpMock.Object, _udpMock.Object);
    }
}
