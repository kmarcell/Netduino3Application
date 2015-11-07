using System;
using Microsoft.SPOT;
using CoreCommunication;

namespace XBee
{
    public class RemoteDeviceFoundEventArgs : EventArgs
    {
        public RemoteXBee device;

        public RemoteDeviceFoundEventArgs(RemoteXBee device)
        {
            this.device = device;
        }
    }

    public delegate void RemoteDeviceFoundEventHandler(RemoteDeviceFoundEventArgs e);

    public class XBeeDiscoveryService
    {
        private XBeeCoordinator coordinator;
        private RemoteXBee[] knownDevices;
        
        public event RemoteDeviceFoundEventHandler RemoteDeviceFound;

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
            if (frame.ATCommandData == null || frame.ATCommandData.Length == 0)
            {
                return true;
            }

            RemoteXBee xbee = new RemoteXBee(frame.SourceAddress, frame.SerialNumber, frame.NodeIdentifier);
            AddXBee(xbee);
            return false;
        }

        private void onRemoteDeviceFound(RemoteXBee xbee)
        {
            RemoteDeviceFoundEventHandler handler = RemoteDeviceFound;
            if (handler != null)
            {
                handler(new RemoteDeviceFoundEventArgs(xbee));
            }
        }

        private void AddXBee(RemoteXBee xbee)
        {
            RemoteXBee[] xbees = new RemoteXBee[knownDevices.Length + 1];
            Array.Copy(knownDevices, xbees, knownDevices.Length);
            xbees[knownDevices.Length] = xbee;
            knownDevices = xbees;
            xbee.Coordinator = coordinator;
            onRemoteDeviceFound(xbee);
        }

        public void Discover()
        {
            coordinator.ReceivedRemoteFrame += new ReceivedRemoteFrameEventHandler(ReceivedRemoteFrameHandler);
            coordinator.StartListen();

            ATCommandRequestFrame discoverNodes = FrameBuilder.ATCommandRequest
                                    .setATCommandName("ND")
                                    .Build() as ATCommandRequestFrame;

            coordinator.EnqueueFrame(discoverNodes, delegate(Frame frame)
            {
                if (!(frame is ATCommandResponseFrame))
                {
                    return false;
                }

                bool endOfDiscovery = HandleNetworkDiscoveryResponse(new NetworkDiscoveryResponseFrame(frame as ATCommandResponseFrame));
                return endOfDiscovery;
            });
        }

        void ReceivedRemoteFrameHandler(object sender, Frame frame)
        {
            if (!(frame is DigitalAnalogSampleFrame)) { return; }
            DigitalAnalogSampleFrame sampleFrame = frame as DigitalAnalogSampleFrame;

            RemoteXBee sourceXBee = null;
            foreach (RemoteXBee xbee in knownDevices)
            {
                if (Frame.isEqualAddress(xbee.SourceAddress64Bit, sampleFrame.SourceAddress64Bit))
                {
                    sourceXBee = xbee;
                }
            }

            if (sourceXBee == null)
            {
                sourceXBee = new RemoteXBee(sampleFrame.SourceAddress16Bit, sampleFrame.SourceAddress64Bit, null);
                AddXBee(sourceXBee);
            }
        }
    }
}
