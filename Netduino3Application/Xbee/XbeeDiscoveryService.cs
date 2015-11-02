using System;
using Microsoft.SPOT;
using CoreCommunication;

namespace XBee
{
    class XBeeDiscoveryService
    {
        private XBeeCoordinator coordinator;
        private RemoteXBee[] knownDevices;

        public RemoteXBee[] KnownDevices
        {
            get { return knownDevices; }
        }

        public XBeeDiscoveryService(XBeeCoordinator coordinator)
        {
            this.coordinator = coordinator;
            knownDevices = new RemoteXBee[0];
        }

        private bool HandleNetworkDiscoveryResponse(NetworkDiscoveryResponseFrame frame)
        {
            if (frame.ATCommandData == null)
            {
                return true;
            }

            RemoteXBee xbee = new RemoteXBee(frame.SourceAddress, frame.SerialNumber, frame.NodeIdentifier);
            AddXBee(xbee);
            return false;
        }

        public void AddXBee(RemoteXBee xbee)
        {
            RemoteXBee[] xbees = new RemoteXBee[knownDevices.Length + 1];
            Array.Copy(knownDevices, xbees, knownDevices.Length);
            xbees[knownDevices.Length] = xbee;
            knownDevices = xbees;

            xbee.coordinator = coordinator;
        }

        public delegate void DiscoveryCallback(RemoteXBee[] knownDevices);
        public void Discover(DiscoveryCallback callback)
        {
            coordinator.StartListen();
            Frame discoverNodes = FrameBuilder.ATCommandRequest
                                    .setATCommandName("ND")
                                    .Build();

            coordinator.EnqueueFrame(discoverNodes, delegate(Frame frame)
            {
                if (!(frame is ATCommandResponseFrame))
                {
                    return false;
                }

                bool endOfDiscovery = HandleNetworkDiscoveryResponse(new NetworkDiscoveryResponseFrame(frame as ATCommandResponseFrame));
                if (endOfDiscovery && callback != null)
                {
                    callback(knownDevices);
                }
                
                return endOfDiscovery;
            });
        }
    }
}
