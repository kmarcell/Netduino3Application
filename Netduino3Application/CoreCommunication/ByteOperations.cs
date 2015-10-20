using System;
using Microsoft.SPOT;

namespace CoreCommunication
{
    public abstract class ByteOperations
    {
        public static UInt16 littleEndianWordFromBytes(byte msb, byte lsb)
        {
            int word = msb * 256 + lsb;
            return (UInt16)word;
        }
    }
}
