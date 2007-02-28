using System;
using System.Collections.Generic;
using System.Text;

namespace EventSystem
{
    using Castle.Core;
    using Castle.Core.Logging;
    using Castle.MicroKernel;
    using Castle.Windsor;
    using Castle.Windsor.Configuration.Interpreters;
    using RakNetDotNet;
    #region Castle MicroKernel
    static class ServiceConfigurator
    {
        static ServiceConfigurator()
        {
            AddComponent<Automobile>();
            // Add more components.
        }
        public static void Dispose()
        {
            container.Dispose();
        }
        #region Generics API.
        public static ServiceType Resolve<ServiceType>()
        {
            return container.Resolve<ServiceType>();
        }
        public static ServiceType Resolve<ServiceType>(System.Collections.IDictionary arguments)
        {
            return (ServiceType)container.Kernel.Resolve(typeof(ServiceType), arguments);
        }
        public static void RegisterCustomDependencies<ServiceType>(System.Collections.IDictionary dependencies)
        {
            container.Kernel.RegisterCustomDependencies(typeof(ServiceType), dependencies);
        }
        public static void AddComponent<ClassType>()
        {
            container.AddComponent(typeof(ClassType).FullName, typeof(ClassType));
        }
        public static void AddComponent<ServiceType, ClassType>()
        {
            container.AddComponent(typeof(ServiceType).FullName, typeof(ServiceType), typeof(ClassType));
        }
        #endregion
        public static IWindsorContainer Container
        {
            get { return container; }
        }
        public static ILoggerFactory LogFactory
        {
            get { return Resolve<ILoggerFactory>(); }
        }
        static readonly IWindsorContainer container = new WindsorContainer(new XmlInterpreter("WindsorConfig.xml"));
    }
    [Singleton]
    class Automobile : IDisposable
    {
        public Automobile(ILogger logger, string _name)
        {
            this.logger = logger;
            logger.Debug("Ctor");
            logger.Warn("first");
            logger.CreateChildLogger("child").Warn("second");
            name = _name;
        }
        public void Drive()
        {
            logger.Debug(name);
        }
        public void Dispose()
        {
            logger.Debug("Dispose");
        }
        readonly ILogger logger;
        string name;
    }

    class Main
    {
        public void Test()
        {
            Dictionary<string, object> dependencies = new Dictionary<string, object>();
            dependencies["_name"] = "mama";
            ServiceConfigurator.RegisterCustomDependencies<Automobile>(dependencies);
            ServiceConfigurator.Resolve<Automobile>().Drive();
            ServiceConfigurator.Dispose();
        }
    }
    #endregion
    class Program
    {
        static void Main(string[] args)
        {
#if true
            Main m = new Main();
            m.Test();
            char keyp = Console.ReadKey(true).KeyChar;
            return;
#endif
            Console.WriteLine("(S)erver or (U)nifiedNetwork or (C)lient?");
            char key = Console.ReadKey(true).KeyChar;
            if (key == 's' || key == 'S')
                ServerMain(args);
            else if (key == 'u' || key == 'U')
                UnifiedNetworkMain(args);
            else
                ClientMain(args);
        }
        static void ClientMain(string[] args)
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
        static void ServerMain(string[] args)
        {
            EventCenterServer server = new EventCenterServer("server.xml");
            RpcCalls rpcCalls = new RpcCalls();
            rpcCalls.ProcessEventOnServerSide += server.ProcessEvent;
            SampleEventFactory factory = new SampleEventFactory();
            rpcCalls.Handler = factory;

            server.Start();

            factory.Dispose();
            server.Dispose();
        }
        #region Unified Network
        static void UnifiedNetworkMain(string[] args)
        {
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
            extendedProperties.Add("allowedPlayers", (ushort)10);
            extendedProperties.Add("port", serverPort);
            UnifiedNetwork unifiedNetwork = new UnifiedNetwork("server.xml", extendedProperties);
            RpcCalls rpcCalls = new RpcCalls();
            rpcCalls.ProcessEventOnServerSide += unifiedNetwork.ProcessEvent;
            SampleEventFactory factory = new SampleEventFactory();
            rpcCalls.Handler = factory;

            if (!isNS)
            {
                unifiedNetwork.ConnectPlayer("127.0.0.1", NAME_SERVICE_PORT);
            }
            System.Console.WriteLine("running... Press space to see status.");
            while (true)
            {
                PrintConnections();
                unifiedNetwork.Update();
                System.Threading.Thread.Sleep(0);
            }

            factory.Dispose();
            unifiedNetwork.Dispose();
        }
        static void PrintConnections()
        {
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
                        SystemAddress systemAddress = UnifiedNetwork.Instance.ServerInterface.GetSystemAddressFromIndex(j);
                        if (!systemAddress.Equals(RakNetBindings.UNASSIGNED_SYSTEM_ADDRESS))
                            Console.Write("{0} ", systemAddress.port);
                    }

                    Console.Write("\n");
                    Console.Write("\n");

                    Console.Write("--------------------------------\n");
                }
                else if (key == 't')
                {
                    IEvent _event = new TestConnectionEvent2((int)SampleEventFactory.EventTypes.TESTCONNECTION2);

                    SampleEventFactory.Instance.StoreExternallyCreatedEvent(_event);
                    UnifiedNetwork.Instance.SendEvent(_event, RakNetBindings.UNASSIGNED_SYSTEM_ADDRESS);
                }
                key = '\0';
            }
        }
        [System.Runtime.InteropServices.DllImport("crtdll.dll")]
        public static extern int _kbhit();  // I do not want to use this.
        static ushort serverPort;
        #endregion
    }
}
