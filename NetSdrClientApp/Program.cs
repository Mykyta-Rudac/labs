using NetSdrClientApp;
using NetSdrClientApp.Networking;

namespace NetSdrClientApp
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static class Program
    {
        public static async Task Main()
        {
            Console.WriteLine(@"Usage:
C - connect
D - disconnet
F - set frequency
S - Start/Stop IQ listener
Q - quit");

            var tcpClient = new TcpClientWrapper("127.0.0.1", 5000);
            var udpClient = new UdpClientWrapper(60000);

            var netSdr = new NetSdrClient(tcpClient, udpClient);

            await new ConsoleRunner(netSdr).RunAsync();
        }

        internal class ConsoleRunner
        {
            private readonly NetSdrClient _netSdr;

            public ConsoleRunner(NetSdrClient netSdr)
            {
                _netSdr = netSdr;
            }

            public async Task RunAsync()
            {
                while (true)
                {
                    var key = Console.ReadKey(intercept: true).Key;
                    if (key == ConsoleKey.C)
                    {
                        await _netSdr.ConnectAsync();
                    }
                    else if (key == ConsoleKey.D)
                    {
                        _netSdr.Disconect();
                    }
                    else if (key == ConsoleKey.F)
                    {
                        await _netSdr.ChangeFrequencyAsync(20000000, 1);
                    }
                    else if (key == ConsoleKey.S)
                    {
                        await HandleStartStopAsync();
                    }
                    else if (key == ConsoleKey.Q)
                    {
                        break;
                    }
                }
            }

            private async Task HandleStartStopAsync()
            {
                if (_netSdr.IQStarted)
                {
                    await _netSdr.StopIQAsync();
                }
                else
                {
                    await _netSdr.StartIQAsync();
                }
            }
        }
    }
}
