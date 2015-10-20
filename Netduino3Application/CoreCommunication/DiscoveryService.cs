using System;
using Microsoft.SPOT;

namespace CoreCommunication
{
    public class RemoteDeviceFountEventArgs : EventArgs
    {
        private object device;

        public RemoteDeviceFountEventArgs(object device)
        {
            this.device = device;
        }

        public object Device
        {
            get { return this.device; }
        }
    }

    public delegate void RemoteDeviceFoundEventHandler(object sender, RemoteDeviceFountEventArgs e);

    interface IDiscoveryService
    {
        void scan();
        void stopScan();

        event RemoteDeviceFoundEventHandler RemoteDeviceFound;
    }
}
