using System;
using Microsoft.SPOT;

namespace XBee
{
    class RemoteXBee
    {
        public byte[] SourceAddress16Bit;
        public byte[] SerialNumber;
        public string Identifier;

        public XBeeCoordinator coordinator;

        public RemoteXBee() { }

        public RemoteXBee(byte[] SourceAddress16Bit, byte[] SerialNumber, string Identifier)
        {
            this.SourceAddress16Bit = SourceAddress16Bit;
            this.SerialNumber = SerialNumber;
            this.Identifier = Identifier;
        }
    }
}
