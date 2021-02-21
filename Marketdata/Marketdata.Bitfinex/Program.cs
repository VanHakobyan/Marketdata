using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using Bitfinex.Client.Websocket;
using Bitfinex.Client.Websocket.Client;
using Bitfinex.Client.Websocket.Messages;
using Bitfinex.Client.Websocket.Requests;
using Bitfinex.Client.Websocket.Requests.Subscriptions;
using Bitfinex.Client.Websocket.Responses;
using Bitfinex.Client.Websocket.Responses.Books;
using Bitfinex.Client.Websocket.Responses.Candles;
using Bitfinex.Client.Websocket.Responses.Configurations;
using Bitfinex.Client.Websocket.Responses.Fundings;
using Bitfinex.Client.Websocket.Responses.Tickers;
using Bitfinex.Client.Websocket.Responses.Trades;
using Bitfinex.Client.Websocket.Responses.Wallets;
using Bitfinex.Client.Websocket.Utils;
using Bitfinex.Client.Websocket.Websockets;
using Serilog;
using Serilog.Events;

namespace Marketdata.Bitfinex
{
    class Program
    {
        private static readonly ManualResetEvent ExitEvent = new ManualResetEvent(false);

        static void Main()
        {
            InitLogging();

            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
            AssemblyLoadContext.Default.Unloading += DefaultOnUnloading;
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;

            Console.WriteLine("|=======================|");
            Console.WriteLine("|    BITFINEX CLIENT    |");
            Console.WriteLine("|=======================|");
            Console.WriteLine();

            var url = BitfinexValues.ApiWebsocketUrl;

            using var communicator = new BitfinexWebsocketCommunicator(url)
            {
                Name = "Bitfinex-1",
                ReconnectTimeout = TimeSpan.FromSeconds(30)
            };
            communicator.ReconnectionHappened.Subscribe(info => Log.Information($"Reconnection happened, type: {info.Type}"));

            using var client = new BitfinexWebsocketClient(communicator);

            client.Streams.InfoStream.Subscribe(info =>
            {
                Log.Information($"Info received version: {info.Version}, reconnection happened, resubscribing to streams");
                SendSubscriptionRequests(client);
            });

            SubscribeToStreams(client);

            communicator.Start();

            ExitEvent.WaitOne();
        }

        private static void SendSubscriptionRequests(BitfinexWebsocketClient client)
        {
            //client.Send(new ConfigurationRequest(ConfigurationFlag.Timestamp | ConfigurationFlag.Sequencing));
            client.Send(new PingRequest() { Cid = 123456 });

            client.Send(new TickerSubscribeRequest("BTC/USD"));
            client.Send(new TickerSubscribeRequest("ETH/USD"));

            client.Send(new TradesSubscribeRequest("BTC/USD"));
            client.Send(new TradesSubscribeRequest("NEC/ETH")); // Nectar coin from ETHFINEX
            client.Send(new FundingsSubscribeRequest("BTC"));
            client.Send(new FundingsSubscribeRequest("USD"));

            client.Send(new CandlesSubscribeRequest("BTC/USD", BitfinexTimeFrame.OneMinute));
            client.Send(new CandlesSubscribeRequest("ETH/USD", BitfinexTimeFrame.OneMinute));

            client.Send(new BookSubscribeRequest("BTC/USD", BitfinexPrecision.P0, BitfinexFrequency.Realtime));
            client.Send(new BookSubscribeRequest("BTC/USD", BitfinexPrecision.P3, BitfinexFrequency.Realtime));
            client.Send(new BookSubscribeRequest("ETH/USD", BitfinexPrecision.P0, BitfinexFrequency.Realtime));

            client.Send(new BookSubscribeRequest("fUSD", BitfinexPrecision.P0, BitfinexFrequency.Realtime));

            client.Send(new RawBookSubscribeRequest("BTCUSD", "100"));
            client.Send(new RawBookSubscribeRequest("fUSD", "25"));
            client.Send(new RawBookSubscribeRequest("fBTC", "25"));

            client.Send(new StatusSubscribeRequest("liq:global"));
            client.Send(new StatusSubscribeRequest("deriv:tBTCF0:USTF0"));
        }

        private static void SubscribeToStreams(BitfinexWebsocketClient client)
        {

            client.Streams.ConfigurationStream.Subscribe(OnNext);

            client.Streams.PongStream.Subscribe(OnNext);

            client.Streams.TickerStream.Subscribe(OnNext);

            client.Streams.TradesSnapshotStream.Subscribe(OnNext);

            client.Streams.FundingStream.Subscribe(OnNext);

            client.Streams.RawBookStream.Subscribe(OnNext);

            client.Streams.CandlesStream.Subscribe(OnNext);

            client.Streams.BookChecksumStream.Subscribe(OnNext);

            client.Streams.WalletStream.Subscribe(OnNext);
        }


        private static void OnNext(Wallet obj)
        {

        }

        private static void OnNext(Trade[] obj)
        {

        }


        private static void OnNext(MessageBase obj)
        {
            if (obj is ConfigurationResponse configurationResponse)
            {

            }
            else if (obj is PongResponse pongResponse)
            {

            }
        }

        private static void OnNext(ResponseBase obj)
        {
            if (obj is Ticker ticker)
            {

            }
            else if (obj is Funding funding)
            {

            }
            else if (obj is RawBook rawBook)
            {

            }
            else if (obj is Candles candles)
            {

            }
            else if (obj is ChecksumResponse checksumResponse)
            {

            }
        }

        private static void InitLogging()
        {
            var executingDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var logPath = Path.Combine(executingDir, "logs", "verbose.log");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .WriteTo.ColoredConsole(LogEventLevel.Debug, outputTemplate:
                    "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }

        private static void CurrentDomainOnProcessExit(object sender, EventArgs eventArgs)
        {
            Log.Warning("Exiting process");
            ExitEvent.Set();
        }

        private static void DefaultOnUnloading(AssemblyLoadContext assemblyLoadContext)
        {
            Log.Warning("Unloading process");
            ExitEvent.Set();
        }

        private static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Log.Warning("Canceling process");
            e.Cancel = true;
            ExitEvent.Set();
        }
    }
}
