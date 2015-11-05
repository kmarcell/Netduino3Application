using System;
using Microsoft.SPOT;

using CoreCommunication;

namespace XBee
{
    class RemoteXBee
    {
        public byte[] SourceAddress16Bit;
        public byte[] SourceAddress64Bit;
        private string identifier;

        public string SerialNumber
        {
            get
            {
                string serialNumber = "";
                foreach (byte b in SourceAddress64Bit)
                {
                    serialNumber += ByteOperations.ByteToHex(b);
                }
                return serialNumber;
            }
        }

        public string Identifier
        {
            get
            {
                if (identifier != null && identifier != "")
                {
                    return identifier;
                }
                return SerialNumber;
            }
        }

        public XBeeCoordinator coordinator;

        public RemoteXBee() { }

        public RemoteXBee(byte[] SourceAddress16Bit, byte[] SourceAddress64Bit, string Identifier)
        {
            this.SourceAddress16Bit = SourceAddress16Bit;
            this.SourceAddress64Bit = SourceAddress64Bit;
            this.identifier = Identifier;
        }
    }
}
