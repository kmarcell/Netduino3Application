using System;
using Microsoft.SPOT;
using CoreCommunication;

namespace Xbee
{
    class XbeeDiscoveryService : IDiscoveryService
    {
        private XbeeDevice coordinator;

        public XbeeDiscoveryService(XbeeDevice coordinator)
        {
            this.coordinator = coordinator;
        }

        public void scan()
        {
            throw new NotImplementedException();
        }

        public void stopScan()
        {
            throw new NotImplementedException();
        }

        public event RemoteDeviceFoundEventHandler RemoteDeviceFound;
    }
}
