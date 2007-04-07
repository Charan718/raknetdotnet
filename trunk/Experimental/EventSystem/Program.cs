using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Castle.Core;
using Castle.Core.Logging;
using RakNetDotNet;
using CommandLine;
using Events;

namespace EventSystem
{
    internal class AppArguments
    {
        [DefaultArgumentAttribute(ArgumentType.Required, HelpText = "Configuration xml filename.")]
        public string ConfigurationFilename;
    }

    sealed class ClientPPLocator : IProtocolProcessorsLocator
    {
        public ClientPPLocator(EventHandlersOnClient handlers)
        {
            EventFactoryOnClient factory = new EventFactoryOnClient();
            ProtocolProcessor processor = new ProtocolProcessor("c", factory, handlers, LightweightContainer.LogFactory.Create(typeof(ProtocolProcessor)));
            processors = new IProtocolProcessor[] {processor};
        }
        private IProtocolProcessor[] processors;

        public IProtocolProcessor[] Processors
        {
            get { return processors; }
        }
    }

    interface IServer
    {
        void Startup();
        void Update();
        void Shutdown();
    }

    [Transient]
    internal sealed class FrontEndServer : IServer
    {
        private readonly ILogger logger;
        private readonly ICommunicator communicator;

        public FrontEndServer(ICommunicator communicator, ILogger logger)
        {
            this.communicator = communicator;
            this.logger = logger;
        }

        public void Startup()
        {
            EventHandlersOnFrontEndServer handlers = new EventHandlersOnFrontEndServer();
            handlers.ConnectionTest += Handlers_OnConnectionTest;
            communicator.ProcessorsLocator = new FrontEndServerPPLocator(handlers);   // inject manually
            communicator.Startup();
        }

        private void Handlers_OnConnectionTest(ConnectionTest t)
        {
            logger.Debug("Handlers_OnConnectionTest");
        }

        public void Update()
        {
            communicator.Update();
        }

        public void Shutdown()
        {
            communicator.Shutdown();
        }
    }

    /// <summary>
    /// test client
    /// </summary>
    [Transient]
    internal sealed class Client : IServer
    {
        private readonly ILogger logger;
        private readonly IClientCommunicator communicator;
        private uint lastSent;

        public Client(IClientCommunicator communicator, ILogger logger)
        {
            this.communicator = communicator;
            this.logger = logger;
        }

        public void Startup()
        {
            EventHandlersOnClient handlers = new EventHandlersOnClient();
            handlers.ConnectionTest += Handlers_OnConnectionTest;
            communicator.ProcessorsLocator = new ClientPPLocator(handlers);   // inject manually
            communicator.Startup();
            communicator.Connect();
        }

        private void Handlers_OnConnectionTest(ConnectionTest t)
        {
            logger.Debug("Handlers_OnConnectionTest");
        }

        public void Update()
        {
            communicator.Update();
            if(4000 < RakNetBindings.GetTime() - lastSent)
            {
                ConnectionTest e = new ConnectionTest();
                communicator.SendEvent(e);
                lastSent = RakNetBindings.GetTime();
                logger.Debug("Sent ConnectionTest.");
            }
        }

        public void Shutdown()
        {
            communicator.Shutdown();
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            AppArguments parsedArgs = new AppArguments();
            if(!Parser.ParseArgumentsWithUsage(args, parsedArgs))
            {
                return;
            }

            LightweightContainer.Configure(parsedArgs.ConfigurationFilename);
            ILogger logger = LightweightContainer.LogFactory.Create("Global");
            IServer server = LightweightContainer.Resolve<IServer>();
            server.Startup();
            logger.Info("Server is started.");
            while(true)
            {
                if(_kbhit() != 0) {
                    char ch = Console.ReadKey(true).KeyChar;
                    if (ch == 'q' || ch == 'Q')
                    {
                        break;
                    }
                }
                server.Update();
            }
            server.Shutdown();
            logger.Info("Server is shutdowned.");
        }

        [Obsolete]
        private static void ClientMain(string[] args)
        {
            // TODO - parse options
            ushort clientPort = 20000;
            string serverIP = "127.0.0.1";

            GameManager game = new GameManager();
            IntroState intro = new IntroState();
            PlayState play = new PlayState(clientPort, serverIP);

            try
            {
                game.Start(intro);

                Console.WriteLine("All done!");
            }
            catch (Exception e)
            {
                Console.WriteLine("An exception occured: {0}", e.ToString());
            }

            game.Dispose();
            intro.Dispose();
            play.Dispose();

            Console.WriteLine("Quiting...");
        }

        [Obsolete]
        private static void ServerMain(string[] args)
        {
            EventCenterServer server = new EventCenterServer("server.xml");
            SampleEventFactory factory = LightweightContainer.Resolve<SampleEventFactory>();
            factory.Reset();
            RpcCalls rpcCalls = LightweightContainer.Resolve<RpcCalls>();
            rpcCalls.Reset();
            rpcCalls.ProcessEventOnServerSide += server.ProcessEvent;

            server.Start();

            factory.Reset();
            server.Dispose();
        }

        #region Unified Network

        [Obsolete]
        private static void UnifiedNetworkMain(string[] args)
        {
            UnifiedNetwork network = LightweightContainer.Resolve<UnifiedNetwork>();
            int c = 0;
#if false
            const ushort NAME_SERVICE_PORT = 6000;
            bool isNS;

            Console.WriteLine("Server Port ? (NS=6000)");
            string input = Console.ReadLine();
            if (input.Equals(string.Empty))
            {
                isNS = true;
                serverPort = NAME_SERVICE_PORT;
            }
            else
            {
                isNS = false;
                serverPort = ushort.Parse(input);
            }

            Dictionary<string, object> extendedProperties = new Dictionary<string, object>();
            extendedProperties.Add("isNS", isNS);
            extendedProperties.Add("allowedPlayers", (ushort) 10);
            extendedProperties.Add("port", serverPort);
            UnifiedNetwork unifiedNetwork = new UnifiedNetwork("server.xml", extendedProperties);
            SampleEventFactory factory = ServiceConfigurator.Resolve<SampleEventFactory>();
            factory.Reset();
            RpcCalls rpcCalls = ServiceConfigurator.Resolve<RpcCalls>();
            rpcCalls.Reset();
            rpcCalls.ProcessEventOnServerSide += unifiedNetwork.ProcessEvent;

            if (!isNS)
            {
                unifiedNetwork.ConnectPlayer("127.0.0.1", NAME_SERVICE_PORT);
            }
            Console.WriteLine("running... Press space to see status.");
            while (true)
            {
                PrintConnections();
                unifiedNetwork.Update();
                Thread.Sleep(0);
            }

            factory.Reset();
            unifiedNetwork.Dispose();
#endif
        }

        [Obsolete]
        private static void SimpleNetworkMain(string[] args)
        {
            
        }

        [Obsolete]
        private static void PrintConnections()
        {
#if false
            if (_kbhit() != 0)
            {
                char key = Console.ReadKey(true).KeyChar;
                if (key == ' ')
                {
                    Console.Write("--------------------------------\n");
                    uint numPeers = UnifiedNetwork.Instance.ServerInterface.NumberOfConnections();
                    //uint numPeers = 10;

                    Console.Write("{0} (Conn): ", serverPort);
                    for (int j = 0; j < numPeers; j++)
                    {
                        SystemAddress systemAddress =
                            UnifiedNetwork.Instance.ServerInterface.GetSystemAddressFromIndex(j);
                        if (!systemAddress.Equals(RakNetBindings.UNASSIGNED_SYSTEM_ADDRESS))
                            Console.Write("{0} ", systemAddress.port);
                    }

                    Console.Write("\n");
                    Console.Write("\n");

                    Console.Write("--------------------------------\n");
                }
                else if (key == 't')
                {
                    IComplecatedEvent _event = new TestConnectionEvent2((int) SampleEventFactory.EventTypes.TESTCONNECTION2);

                    ServiceConfigurator.Resolve<SampleEventFactory>().StoreExternallyCreatedEvent(_event);
                    UnifiedNetwork.Instance.SendEvent(_event, RakNetBindings.UNASSIGNED_SYSTEM_ADDRESS);
                }
                key = '\0';
            }
#endif
        }

        [DllImport("crtdll.dll")]
        public static extern int _kbhit(); // I do not want to use this.
        private static ushort serverPort;

        #endregion
    }
}