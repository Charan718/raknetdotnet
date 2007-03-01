using System;
using System.Collections.Generic;
using System.Text;

namespace EventSystem
{
    using System.Diagnostics;
    using RakNetDotNet;
    using Castle.Core;

    delegate void ProcessEventDelegate(IEvent _event);

    [Singleton]
    sealed class RpcCalls
    {
        public static void SendEventToClient(RPCParameters _params)
        {
            BitStream source = new BitStream(_params, false);
            RpcCalls instance = ServiceConfigurator.Resolve<RpcCalls>();
            IEvent _event = instance.RecreateEvent(source);

            Debug.Assert(instance.ProcessEventOnClientSide != null);
            instance.ProcessEventOnClientSide(_event);

            instance.WipeEvent(_event);
        }
        public static void SendEventToServer(RPCParameters _params)
        {
            SystemAddress sender = _params.sender;

            BitStream source = new BitStream(_params, false);

            RpcCalls instance = ServiceConfigurator.Resolve<RpcCalls>();
            IEvent _event = instance.RecreateEvent(source);
            if (false) Console.WriteLine("EventCenterServer> {0}", _event.ToString());
            _event.OriginPlayer = sender;
            Debug.Assert(instance.ProcessEventOnServerSide != null);
            instance.ProcessEventOnServerSide(_event);
        }
        public void Reset()
        {
            ProcessEventOnClientSide = null;
            ProcessEventOnServerSide = null;
            factory = null;
        }
        public IEvent RecreateEvent(BitStream source)
        {
            return factory.RecreateEvent(source);
        }
        public void WipeEvent(IEvent _event)
        {
            factory.WipeEvent(_event);
        }
        public AbstractEventFactory Handler
        {
            set { factory = value; }
        }
        /// <summary>
        /// if on sever-side then null
        /// </summary>
        public event ProcessEventDelegate ProcessEventOnClientSide;
        /// <summary>
        /// if on client-side then null
        /// </summary>
        public event ProcessEventDelegate ProcessEventOnServerSide;
        AbstractEventFactory factory;
    }
}
