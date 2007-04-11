using Castle.Core;
using Castle.Core.Logging;

namespace EventSystem
{
    [Transient]
    internal  class ClientDOManager : DOManager, IClientDOManager
    {
        private IClientCommunicator comm;

        public ClientDOManager(ILogger logger)
            : base(logger)
        {
        }

        public IClientCommunicator ClientCommunicator
        {
            get { return comm; }
            set { comm = value; }
        }

        public void StoreObject(IDObject dObject)
        {
            if (!dObjects.ContainsValue(dObject))
            {
                dObjects.Add(dObject.OId, dObject);
                return;
            }
            logger.Error("Duplicate DObject found.", dObject.OId);
        }

        public override void SendEvent(IEvent e)
        {
            ClientCommunicator.SendEvent(e);
        }

        public override void PostEvent(IEvent e)
        {
            GetObject(e.TargetOId).HandleEvent(e);
        }
    }
}