using System;
using System.Collections.Generic;
using System.Text;
using RakNetDotNet;

namespace StressTest
{
    class Program
    {
        static void Main(string[] args)
        {
            TelnetTransport tt = new TelnetTransport();
            TestCommandServer(tt, 23);
        }

        static void TestCommandServer(TransportInterface ti, ushort port)
        {
            ConsoleServer consoleServer = new ConsoleServer();
            RakNetCommandParser rcp = new RakNetCommandParser();
            LogCommandParser lcp = new LogCommandParser();
            uint lastlog = 0;
            RakPeerInterface rakPeer = RakNetworkFactory.GetRakPeerInterface();
            IntPtr testChannel = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi("TestChannel");  // you must call FreeHGlobal

            consoleServer.AddCommandParser(rcp);
            consoleServer.AddCommandParser(lcp);
            consoleServer.SetTransportProvider(ti, port);
            rcp.SetRakPeerInterface(rakPeer);
            lcp.AddChannel(testChannel);
            while (true)
            {
                consoleServer.Update();

                if (RakNetDotNet.RakNet.GetTime() > lastlog + 4000)
                {
                    lcp.WriteLog(testChannel, "Test of logger");
                    lastlog = RakNetDotNet.RakNet.GetTime();
                }

                System.Threading.Thread.Sleep(30);
            }
        }
    }
}